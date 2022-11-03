using System.Net.Http.Json;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Common.Util;
using Dan.Core.Config;
using Polly;
using Polly.Registry;

namespace Dan.Core.Services;
public class CachingEntityRegistryApiClientService : IEntityRegistryApiClientService
{
    public const string EntityRegistryCachePolicy = "EntityRegistryCachePolicy";

    private readonly IHttpClientFactory _clientFactory;
    private readonly IPolicyRegistry<string> _policyRegistry;
    private readonly KeyedLock<string> _keyedLock = new();

    public CachingEntityRegistryApiClientService(IHttpClientFactory clientFactory, IPolicyRegistry<string> policyRegistry)
    {
        _clientFactory = clientFactory;
        _policyRegistry = policyRegistry;
    }

    public async Task<UpstreamEntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        // Short circuit checks for organization numbers that are configured to be test numbers.
        var organizationNumber = registryApiUri.AbsolutePath.Split('/').Last();
        if (Settings.IsDevEnvironment && Settings.TestEnvironmentValidOrgs.Contains(organizationNumber))
        {
            return new UpstreamEntityRegistryUnit()
            {
                Organisasjonsnummer = Convert.ToInt32(organizationNumber),
                Organisasjonsform = new Organisasjonsform { Kode = "STAT" },
                Navn = "TESTEORGANISASJON",
            };
        }

        var cacheKey = GetCacheKeyFromUri(registryApiUri);
        try
        {
            // There may be several parallel requests due to authorization requirements being validated in parallel. 
            // To avoid unecessary requests for the same unit, use a keyed lock to make sure we hit the cache.
            await _keyedLock.WaitAsync(cacheKey);

            var cachePolicy = _policyRegistry.Get<AsyncPolicy<UpstreamEntityRegistryUnit?>>(EntityRegistryCachePolicy);
            return await cachePolicy.ExecuteAsync(
                async _ => await InternalGetUpstreamEntityRegistryUnitAsync(registryApiUri), new Context(cacheKey));
        }
        finally
        {
            _keyedLock.Release(cacheKey);
        }
    }

    private static string GetCacheKeyFromUri(Uri registryApiUri)
    {
        return "_ccr_" + registryApiUri;
    }

    private async Task<UpstreamEntityRegistryUnit?> InternalGetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        var client = _clientFactory.CreateClient("entityRegistryClient");
        var request = new HttpRequestMessage(HttpMethod.Get, registryApiUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<UpstreamEntityRegistryUnit>();
    }
}
