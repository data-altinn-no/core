using System.Net.Http.Json;
using Dan.Common.Interfaces;

namespace Dan.Common.Services;
public class DefaultEntityRegistryApiClientService : IEntityRegistryApiClientService
{
    private readonly IHttpClientFactory _clientFactory;

    public DefaultEntityRegistryApiClientService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    public async Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        var client = _clientFactory.CreateClient("entityRegistryClient");
        var request = new HttpRequestMessage(HttpMethod.Get, registryApiUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        return await response.Content.ReadFromJsonAsync<EntityRegistryUnit>();
    }
}
