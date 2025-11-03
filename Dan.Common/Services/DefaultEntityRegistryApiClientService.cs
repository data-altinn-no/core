using System.Net.Http.Json;
using Dan.Common.Interfaces;

namespace Dan.Common.Services;
/// <summary>
/// Default implementation of IEntityRegistryApiClientService
/// </summary>
[Obsolete("Should not be used, use Dan.Common.Services.ICcrClientService for fetching data from CCR")]
public class DefaultEntityRegistryApiClientService : IEntityRegistryApiClientService
{
    private readonly IHttpClientFactory _clientFactory;

    /// <summary>
    /// Default constructor, sets _clientfactory to provided IHttpClientFactory
    /// </summary>
    public DefaultEntityRegistryApiClientService(IHttpClientFactory clientFactory)
    {
        _clientFactory = clientFactory;
    }

    /// <summary>
    /// Get entity registry unit
    /// </summary>
    public async Task<EntityRegistryUnit?> GetUpstreamEntityRegistryUnitAsync(Uri registryApiUri)
    {
        var client = _clientFactory.CreateClient("entityRegistryClient");
        var request = new HttpRequestMessage(HttpMethod.Get, registryApiUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return null;

        var responseString = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<EntityRegistryUnit>(responseString);
    }
}
