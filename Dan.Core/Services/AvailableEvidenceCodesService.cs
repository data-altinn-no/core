using Dan.Common.Models;
using Dan.Common.Extensions;
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
using Azure.Identity;

namespace Dan.Core.Services;

public class AvailableEvidenceCodesService(
    ILoggerFactory loggerFactory,
    IHttpClientFactory httpClientFactory,
    IPolicyRegistry<string> policyRegistry,
    IDistributedCache distributedCache,
    IServiceContextService serviceContextService,
    IFunctionContextAccessor functionContextAccessor)
    : IAvailableEvidenceCodesService
{
    public static TimeSpan DistributedCacheTtl = TimeSpan.FromHours(12);
    private readonly ILogger<IAvailableEvidenceCodesService> _logger = loggerFactory.CreateLogger<AvailableEvidenceCodesService>();

    private readonly AsyncNonKeyedLocker semaphore = new(1);
    private const int MemoryCacheTtlSeconds = 600;

    private const string CachingPolicy = "EvidenceCodesCachePolicy";
    private const string HttpClientName = "EvidenceCodesClient";
    private const string CacheContextKey = "AvailableEvidenceCodes";
    private const string CacheResponseHeader = "x-cache";

    /// <summary>
    /// Gets the list of current active evidence codes. This endpoint can be hit several times during a request.
    /// </summary>
    /// <param name="forceRefresh">If true will evict the current cache (both in-memory and distributed) and force a source-level refresh</param>
    /// <returns>A list of active evidence codes</returns>
    public async Task<List<EvidenceCode>> GetAvailableEvidenceCodes(bool forceRefresh = false)
    {
        List<EvidenceCode>? evidenceCodes;
        if (!forceRefresh)
        {
            evidenceCodes = await distributedCache.GetValueAsync<List<EvidenceCode>>(CacheContextKey);
            if (evidenceCodes is not null)
            {
                SetCacheDiagnosticsHeader("hit-distributed");
                evidenceCodes = FilterEvidenceCodes(evidenceCodes);
                return evidenceCodes;
            }
        }

        if (forceRefresh)
        {
            SetCacheDiagnosticsHeader("force-evict");
        }

        using (await semaphore.LockAsync())
        {
            if (!forceRefresh)
            {
                // Checking if another thread finished caching
                evidenceCodes = await distributedCache.GetValueAsync<List<EvidenceCode>>(CacheContextKey);
                if (evidenceCodes is not null)
                {
                    evidenceCodes = FilterEvidenceCodes(evidenceCodes);
                    return evidenceCodes;
                }
            }
            evidenceCodes = await GetAvailableEvidenceCodesFromEvidenceSources();
            await distributedCache.SetValueAsync(CacheContextKey, evidenceCodes);
            evidenceCodes = FilterEvidenceCodes(evidenceCodes);
            return evidenceCodes;
        }
    }

    public async Task<Dictionary<string, string>> GetAliases()
    {
        var aliases = new Dictionary<string, string>();
        var availableEvienceCodes = await distributedCache.GetValueAsync<List<EvidenceCode>>(CacheContextKey);
        if (availableEvienceCodes is null)
        {
            return aliases;
        }
        var aliasedEvidenceCodes = availableEvienceCodes
            .Where(ec => ec.DatasetAliases is not null && ec.DatasetAliases.Count > 0);
        foreach (var aliasedEvidenceCode in aliasedEvidenceCodes)
        {
            foreach (var alias in aliasedEvidenceCode.DatasetAliases!)
            {
                aliases.Add(alias.DatasetAliasName, aliasedEvidenceCode.EvidenceCodeName);
            }
        }

        return aliases;
    }

    private void SetCacheDiagnosticsHeader(string value, bool overwrite = false)
    {
        var requestContextService = functionContextAccessor.FunctionContext?.InstanceServices.GetService<IRequestContextService>();
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
        var serviceContexts = await serviceContextService.GetRegisteredServiceContexts();
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
        var client = httpClientFactory.CreateClient(HttpClientName);
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
        foreach (var es in evidenceCodes)
        {
            es.AuthorizationRequirements.ForEach(x => x.RequirementType = x.GetType().Name);
        }
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
                aliasedEvidenceCode.ServiceContext = alias.ServiceContext;
                aliasedEvidenceCode.BelongsToServiceContexts = [alias.ServiceContext];
                aliasedEvidenceCode.EvidenceCodeName = alias.DatasetAliasName;
                aliasedEvidenceCode.DatasetAliases = null;
                aliasedEvidenceCode.AuthorizationRequirements = aliasedEvidenceCode
                    .AuthorizationRequirements
                    .Where(a =>
                        a.AppliesToServiceContext.Count == 0 ||
                        a.AppliesToServiceContext.Contains(alias.ServiceContext))
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
