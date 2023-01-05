using AsyncKeyedLock;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;

namespace Dan.Core.Services;
public class CachingEntityRegistryApiClientService : IEntityRegistryApiClientService
{
    public const string EntityRegistryCachePolicy = "EntityRegistryCachePolicy";

    private readonly ILogger<CachingEntityRegistryApiClientService> _logger;
    private readonly IHttpClientFactory _clientFactory;
    private readonly IPolicyRegistry<string> _policyRegistry;
    private readonly AsyncKeyedLocker<string> _asyncKeyedLock = new(o => o.PoolSize = 10);

    public CachingEntityRegistryApiClientService(ILoggerFactory loggerFactory, IHttpClientFactory clientFactory, IPolicyRegistry<string> policyRegistry)
    {
        _logger = loggerFactory.CreateLogger<CachingEntityRegistryApiClientService>();
        _clientFactory = clientFactory;
        _policyRegistry = policyRegistry;
    }

    public async Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        // Short circuit checks for organization numbers that are configured to be test numbers.
        var organizationNumber = registryApiUri.AbsolutePath.Split('/').Last();
        if (Settings.IsDevEnvironment && Settings.TestEnvironmentValidOrgs.Contains(organizationNumber))
        {
            return new EntityRegistryUnit()
            {
                Organisasjonsnummer = organizationNumber,
                Organisasjonsform = new Organisasjonsform { Kode = "STAT" },
                Navn = "TESTEORGANISASJON",
            };
        }

        var cacheKey = GetCacheKeyFromUri(registryApiUri);

        // There may be several parallel requests due to authorization requirements being validated in parallel. 
        // To avoid unecessary requests for the same unit, use a keyed lock to make sure we hit the cache.
        using (await _asyncKeyedLock.LockAsync(cacheKey))
        {
            var cachePolicy = _policyRegistry.Get<AsyncPolicy<EntityRegistryUnit?>>(EntityRegistryCachePolicy);
            return await cachePolicy.ExecuteAsync(
                async _ => await InternalGetUpstreamEntityRegistryUnitAsync(registryApiUri), new Context(cacheKey));
        }
    }

    private static string GetCacheKeyFromUri(Uri registryApiUri)
    {
        return "_ccr_" + registryApiUri;
    }

    private async Task<EntityRegistryUnit?> InternalGetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        var client = _clientFactory.CreateClient("entityRegistryClient");
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
            _logger.LogError("Failed deserializing from BR on {registryApiUri}", registryApiUri);
            throw;
        }
    }
}
