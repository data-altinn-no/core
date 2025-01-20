using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using System.Text;
using Dan.Core.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using AsyncKeyedLock;

namespace Dan.Core.Services;

public class AvailableEvidenceCodesService : IAvailableEvidenceCodesService
{
    public static TimeSpan DistributedCacheTtl = TimeSpan.FromHours(12);

    private readonly ILogger<IAvailableEvidenceCodesService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPolicyRegistry<string> _policyRegistry;
    private readonly IDistributedCache _distributedCache;
    private readonly IServiceContextService _serviceContextService;

    private List<EvidenceCode> _memoryCache = new();
    private DateTime _updateMemoryCache = DateTime.MinValue;
    private readonly AsyncNonKeyedLocker _semaphoreForceRefresh = new(1);
    private readonly AsyncNonKeyedLocker _semaphore = new(1);
    private readonly IFunctionContextAccessor _functionContextAccessor;
    private const int MemoryCacheTtlSeconds = 120;

    private const string CachingPolicy = "EvidenceCodesCachePolicy";
    private const string HttpClientName = "EvidenceCodesClient";
    private const string CacheContextKey = "AvailableEvidenceCodes";
    private const string CacheResponseHeader = "x-cache";

    public AvailableEvidenceCodesService(
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory,
        IPolicyRegistry<string> policyRegistry,
        IDistributedCache distributedCache,
        IServiceContextService serviceContextService,
        IFunctionContextAccessor functionContextAccessor)
    {
        _logger = loggerFactory.CreateLogger<AvailableEvidenceCodesService>();
        _httpClientFactory = httpClientFactory;
        _policyRegistry = policyRegistry;
        _distributedCache = distributedCache;
        _serviceContextService = serviceContextService;
        _functionContextAccessor = functionContextAccessor;
    }

    /// <summary>
    /// Gets the list of current active evidence codes. This endpoint can be hit several times during a request. In order to reduce I/O to the distributed cache, it employs
    /// an additional layer of caching via memory. Uses semaphores to handle concurrent writes to the caches. 
    /// </summary>
    /// <param name="forceRefresh">If true will evict the current cache (both in-memory and distributed) and force a source-level refresh</param>
    /// <returns>A list of active evidence codes</returns>
    public async Task<List<EvidenceCode>> GetAvailableEvidenceCodes(bool forceRefresh = false)
    {
        // Cache still valid
        if (!forceRefresh && DateTime.UtcNow < _updateMemoryCache)
        {
            SetCacheDiagnosticsHeader("hit-local");
            return FilterEvidenceCodes(_memoryCache);
        }

        if (forceRefresh)
        {
            // Force refresh has been called. This is only performed manually or in conjuction with a deploy.
            // Use a separate semaphore to ensure only a single thread can do this at a time without blocking other requests
            using (await _semaphoreForceRefresh.LockAsync())
            {
                await RefreshEvidenceCodesCache();
                return FilterEvidenceCodes(_memoryCache);
            }
        }

        // The memory cache is expired. We do not know if Redis cache is expired, as this is handled by Polly.
        using (await _semaphore.LockAsync())
        {
            // Recheck if another thread has updated the memory cache while we were waiting for the semaphore
            if (DateTime.UtcNow < _updateMemoryCache)
            {
                SetCacheDiagnosticsHeader("hit-local-late");
                return FilterEvidenceCodes(_memoryCache);
            }

            // This uses Polly to get from the distributed cache, or refresh from source if Redis cache is expired.
            _memoryCache = await GetAvailableEvidenceCodesFromDistributedCache();
            _updateMemoryCache = DateTime.UtcNow.AddSeconds(MemoryCacheTtlSeconds);

            return FilterEvidenceCodes(_memoryCache);
        }
    }

    public Dictionary<string, string> GetAliases()
    {
        var aliases = new Dictionary<string, string>();
        var aliasedEvidenceCodes = _memoryCache
            .Where(ec => ec.DatasetAliases is not null && ec.DatasetAliases.Count > 0);
        foreach (var aliasedEvidenceCode in aliasedEvidenceCodes)
        {
            foreach (var alias in aliasedEvidenceCode.DatasetAliases!)
            {
                aliases.Add(alias.Value, aliasedEvidenceCode.EvidenceCodeName);
            }
        }

        return aliases;
    }

    private void SetCacheDiagnosticsHeader(string value, bool overwrite = false)
    {
        var requestContextService = _functionContextAccessor.FunctionContext?.InstanceServices.GetService<IRequestContextService>();
        if (requestContextService == null) return;
        if (overwrite)
        {
            requestContextService.CustomResponseHeaders[CacheResponseHeader] = value;
        }
        else
        {
            requestContextService.CustomResponseHeaders.TryAdd(CacheResponseHeader, value);
        }

    }

    /// <summary>
    /// This fetches evidence codes from the sources and updates the distributed and in-memory caches. 
    /// </summary>
    /// <returns>Nothing</returns>
    private async Task RefreshEvidenceCodesCache()
    {
        var evidenceCodes = await GetAvailableEvidenceCodesFromEvidenceSources();
        SetCacheDiagnosticsHeader("force-evict");
        if (evidenceCodes.Count == 0)
        {
            _logger.LogWarning("Failed to refresh evidence codes cache, received empty list");
            return;
        }

        //  Add some metadata properties to make serialized output more parseable
        foreach (var es in evidenceCodes)
        {
            es.AuthorizationRequirements.ForEach(x => x.RequirementType = x.GetType().Name);
        }

        await _distributedCache.SetAsync(CacheContextKey, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(
            evidenceCodes,
            new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            })),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = DistributedCacheTtl
            });

        _memoryCache = evidenceCodes;
        _updateMemoryCache = DateTime.UtcNow.AddSeconds(MemoryCacheTtlSeconds);
    }

    private async Task<List<EvidenceCode>> GetAvailableEvidenceCodesFromDistributedCache()
    {
        SetCacheDiagnosticsHeader("hit-distributed");
        var cachePolicy = _policyRegistry.Get<AsyncPolicy<List<EvidenceCode>>>(CachingPolicy);
        return await cachePolicy.ExecuteAsync(
            async _ => await GetAvailableEvidenceCodesFromEvidenceSources(), new Context(CacheContextKey));
    }

    private async Task<List<EvidenceCode>> GetAvailableEvidenceCodesFromEvidenceSources()
    {
        SetCacheDiagnosticsHeader("miss", overwrite: true);
        using (var _ = _logger.Timer($"availableevidence-cache-refresh"))
        {
            var sources = GetEvidenceSources();
            var evidenceCodes =
                await Task.WhenAll(sources.Select(async source => await GetEvidenceCodesFromSource(source)));
            var evidenceCodesFlattened = evidenceCodes.SelectMany(x => x).ToList();
            await AddServiceContextAuthorizationRequirements(evidenceCodesFlattened);
            return evidenceCodesFlattened;
        }
    }

    private async Task AddServiceContextAuthorizationRequirements(List<EvidenceCode> evidenceCodes)
    {
        var serviceContexts = await _serviceContextService.GetRegisteredServiceContexts();
        foreach (var serviceContext in serviceContexts)
        {
            if (!serviceContext.AuthorizationRequirements.Any()) continue;
            foreach (var evidenceCode in evidenceCodes)
            {
                if (!evidenceCode.GetBelongsToServiceContexts().Contains(serviceContext.Name)) continue;

                var serviceContextRequirements = serviceContext.AuthorizationRequirements.DeepCopy();
                serviceContextRequirements.ForEach(x => x.AppliesToServiceContext = new List<string> { serviceContext.Name });

                evidenceCode.AuthorizationRequirements.AddRange(serviceContextRequirements);
            }
        }
    }

    private async Task<List<EvidenceCode>> GetEvidenceCodesFromSource(EvidenceSource source)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, source.Url);
            request.Headers.Add("Accept", "application/json");
            var response = await client.SendAsync(request);
            var result = await response.Content.ReadAsStringAsync();
            var list = JsonConvert.DeserializeObject<List<EvidenceCode>>(result,
                new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                    NullValueHandling = NullValueHandling.Ignore,
                    SerializationBinder = new ValidJsonTypesSerializationBinder()
                }
            );

            if (list == null)
            {
                return new List<EvidenceCode>();
            }

            list.ForEach(x => x.EvidenceSource = source.Provider);
            return list;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                $"ES did not return evidencecode metadata, source: {source}, sourceurl: {source.Url}, exception: {ex.Message}");
            return new List<EvidenceCode>();
        }
    }

    private static List<EvidenceSource> GetEvidenceSources()
    {
        var sources = new List<EvidenceSource>();
        foreach (string evidenceSource in Settings.EvidenceSources)
        {
            var url = Settings.GetEvidenceSourceUrl(evidenceSource);
            sources.Add(new EvidenceSource() { Url = url, Provider = evidenceSource });
        }

        return sources;
    }

    private static List<EvidenceCode> FilterEvidenceCodes(IEnumerable<EvidenceCode> evidenceCodes)
    {
        evidenceCodes = FilterInactive(evidenceCodes);
        evidenceCodes = SplitAliases(evidenceCodes);
        return evidenceCodes.ToList();
    }
    private static List<EvidenceCode> FilterInactive(IEnumerable<EvidenceCode> evidenceCodes)
    {
        return evidenceCodes.Where(IsDatasetValid).ToList();
    }

    private static List<EvidenceCode> SplitAliases(IEnumerable<EvidenceCode> evidenceCodes)
    {
        var evidenceCodesList = evidenceCodes.ToList();
        var aliasedEvidenceCodes = evidenceCodesList.Where(e => e.DatasetAliases != null && e.DatasetAliases.Count != 0).ToList();
        var splitEvidenceCodes = new List<EvidenceCode>();
        foreach (var evidenceCode in aliasedEvidenceCodes)
        {
            foreach (var alias in evidenceCode.DatasetAliases!)
            {
                var aliasedEvidenceCode = evidenceCode.DeepCopy();
                aliasedEvidenceCode.ServiceContext = alias.Key;
                aliasedEvidenceCode.BelongsToServiceContexts = [alias.Key];
                aliasedEvidenceCode.EvidenceCodeName = alias.Value;
                aliasedEvidenceCode.DatasetAliases = null;
                aliasedEvidenceCode.AuthorizationRequirements = aliasedEvidenceCode
                    .AuthorizationRequirements
                    .Where(a =>
                        a.AppliesToServiceContext.Count == 0 ||
                        a.AppliesToServiceContext.Contains(alias.Key))
                    .ToList();
                
                splitEvidenceCodes.Add(aliasedEvidenceCode);
            }

            evidenceCodesList.Remove(evidenceCode);
        }
        evidenceCodesList.AddRange(splitEvidenceCodes);
        return evidenceCodesList;
    }

    private static bool IsDatasetValid(EvidenceCode dataSet)
    {
        if (dataSet.ValidFrom != null)
        {
            if (dataSet.ValidFrom >= DateTime.Now)
            {
                // Dataset is not valid yet
                return false;
            }
        }

        if (dataSet.ValidTo != null)
        {
            if (dataSet.ValidTo <= DateTime.Now)
            {
                // Dataset is no longer valid
                return false;
            }
        }

        return true;
    }

    private class EvidenceSource
    {
        /// <summary>
        /// The url to the evidence source
        /// </summary>
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// The provider of the evidence source
        /// </summary>
        public string Provider { get; set; } = string.Empty;
    }
}
