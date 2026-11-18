using System.Text.RegularExpressions;
using AsyncKeyedLock;
using Dan.Common.Extensions;
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
    : Interfaces.IEntityRegistryApiClientService
{
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
            var result = await InternalGetUpstreamEntityRegistryUnitAsync(registryApiUri);
            var ttl = result is null ? TimeSpan.FromMinutes(5) : TimeSpan.FromHours(12);
            var options = new DistributedCacheEntryOptions().SetAbsoluteExpiration(ttl);
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
}
