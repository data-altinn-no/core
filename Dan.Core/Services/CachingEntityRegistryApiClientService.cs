using System.Diagnostics;
using System.Text.RegularExpressions;
using AsyncKeyedLock;
using Dan.Common.Extensions;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Dan.Core.Services;
public partial class CachingEntityRegistryApiClientService(
    ILoggerFactory loggerFactory,
    IHttpClientFactory clientFactory,
    IDistributedCache distributedCache)
    : IEntityRegistryApiClientService
{
    public const string EntityRegistryCachePolicy = "EntityRegistryCachePolicy";
    public const string EntityRegistryListCachePolicy = "EntityRegistryListCachePolicy";

    private readonly ILogger<CachingEntityRegistryApiClientService> logger = loggerFactory.CreateLogger<CachingEntityRegistryApiClientService>();
    private readonly AsyncKeyedLocker<string> asyncKeyedLock = new(o => o.PoolSize = 10);

    public async Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        // Short circuit checks for organization numbers that are configured to be test numbers.
        var organizationNumber = registryApiUri.AbsolutePath.Split('/').Last();
        if (Settings.IsDevEnvironment && Settings.TestEnvironmentValidOrgs.Contains(organizationNumber))
        {
            return new EntityRegistryUnit()
            {
                Organisasjonsnummer = organizationNumber,
                Organisasjonsform = new Organisasjonsform { Beskrivelse = "Beskrivelse", Kode = "STAT", Links = new Links { Self = new Link { Href = new Uri("https://data.altinn.no") } } },
                Navn = "TESTEORGANISASJON",
                Links = new Links { Self = new Link { Href = new Uri("https://data.altinn.no") } },
                Underenheter = []
            };
        }

        var cacheKey = GetCacheKeyFromUri(registryApiUri);

        // There may be several parallel requests due to authorization requirements being validated in parallel. 
        // To avoid unecessary requests for the same unit, use a keyed lock to make sure we hit the cache.
        using (await asyncKeyedLock.LockAsync(cacheKey))
        {
            var cacheHit = await distributedCache.GetValueAsync<EntityRegistryUnit>(cacheKey);
            if (cacheHit is not null)
            {
                return cacheHit;
            }
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));
            var result = await InternalGetUpstreamEntityRegistryUnitAsync(registryApiUri);
            await distributedCache.SetValueAsync(cacheKey, result, options);
            return result;
        }
    }
    
    public async Task<List<EntityRegistryUnit>> GetUpstreamEntityRegistryUnitsAsync(Uri registryApiUri)
    {
        // Short circuit checks for organization numbers that are configured to be test numbers.
        var match = MainUnitQueryParamRegex().Match(registryApiUri.Query);
        var organizationNumber = match.Groups[1].Value;
        if (Settings.IsDevEnvironment && Settings.TestEnvironmentValidOrgs.Contains(organizationNumber))
        {
            if (organizationNumber == "111111111")
            {
                return [];
            }
            
            return
            [
                new EntityRegistryUnit
                {
                    Organisasjonsnummer = "111111111",
                    Organisasjonsform = new Organisasjonsform { Beskrivelse = "Beskrivelse", Kode = "STAT", Links = new Links { Self = new Link { Href = new Uri("https://data.altinn.no") } } },
                    OverordnetEnhet = organizationNumber,
                    Navn = "TESTEORGANISASJON",
                    Underenheter = [],
                    Links = new Links { Self = new Link { Href = new Uri("https://data.altinn.no") } }
                }
            ];
        }

        var cacheKey = GetCacheKeyFromUri(registryApiUri);
        
        // There may be several parallel requests due to authorization requirements being validated in parallel. 
        // To avoid unecessary requests for the same unit, use a keyed lock to make sure we hit the cache.
        using (await asyncKeyedLock.LockAsync(cacheKey))
        {
            var cacheHit = await distributedCache.GetValueAsync<List<EntityRegistryUnit>>(cacheKey);
            if (cacheHit is not null)
            {
                return cacheHit;
            }
            
            var options = new DistributedCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromHours(12));
            var result = await InternalGetUpstreamEntityRegistryUnitsAsync(registryApiUri);
            await distributedCache.SetValueAsync(cacheKey, result, options);
            return result;
        }
    }

    private static string GetCacheKeyFromUri(Uri registryApiUri)
    {
        return "_ccr_" + registryApiUri;
    }

    private async Task<EntityRegistryUnit?> InternalGetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        var client = clientFactory.CreateClient("entityRegistryClient");
        var request = new HttpRequestMessage(HttpMethod.Get, registryApiUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var responseString = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonConvert.DeserializeObject<EntityRegistryUnit>(responseString);
        }
        catch (Exception)
        {
            logger.LogError("Failed deserializing from BR on {registryApiUri}", registryApiUri);
            throw;
        }
    }
    
    private async Task<List<EntityRegistryUnit>> InternalGetUpstreamEntityRegistryUnitsAsync(Uri registryApiUri)
    {
        var stopwatch = Stopwatch.StartNew();
        var client = clientFactory.CreateClient("entityRegistryClient");
        var nextUri = registryApiUri;
        var subUnits = new List<EntityRegistryUnit>();
        do
        {
            var request = new HttpRequestMessage(HttpMethod.Get, nextUri);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return [];
            
            var responseString = await response.Content.ReadAsStringAsync();
            var subunitsPage = JsonConvert.DeserializeObject<BrregPage<Subunits>>(responseString);
            subUnits.AddRange(subunitsPage?.Embedded?.SubUnits ?? []);
            
            nextUri = subunitsPage?.Links?.Next?.Href is null ? null : new Uri(subunitsPage.Links.Next.Href);
        } while (nextUri is not null);
        
        return subUnits;
    }

    [GeneratedRegex(@"overordnetEnhet=(\d+)")]
    private static partial Regex MainUnitQueryParamRegex();
}
