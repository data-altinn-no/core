using System.Net.Http.Json;
using Dan.Common.Interfaces;

namespace Dan.Common.Services;
/// <summary>
/// Default implementation of IEntityRegistryApiClientService
/// </summary>
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
    
    /// <summary>
    /// Get list of entity registry units
    /// </summary>
    public async Task<List<EntityRegistryUnit>> GetUpstreamEntityRegistryUnitsAsync(Uri registryApiUri)
    {
        var client = _clientFactory.CreateClient("entityRegistryClient");
        var request = new HttpRequestMessage(HttpMethod.Get, registryApiUri);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");

        var response = await client.SendAsync(request);
        if (!response.IsSuccessStatusCode) return [];

        // TODO: Handle pagination
        var responseString = await response.Content.ReadAsStringAsync();
        var subunitsPage = JsonConvert.DeserializeObject<BrregPage<Subunits>>(responseString);
        return subunitsPage!.Embedded.SubUnits;
    }
}

// TODO: Move, temp location
public class BrregPage<T> where T : class
{
    [JsonProperty("_embedded")]
    public T Embedded { get; set; }
    
    [JsonProperty("_links")]
    public BrregLinks Links { get; set; }
    
    [JsonProperty("page")]
    public BrregPageInfo Page { get; set; }
}

public class Subunits
{
    [JsonProperty("underenheter")]
    public List<EntityRegistryUnit> SubUnits { get; set; }
}

public class BrregPageInfo
{
    [JsonProperty("size")]
    public int Size { get; set; }
    
    [JsonProperty("totalElements")]
    public int TotalElements { get; set; }
    
    [JsonProperty("totalPages")]
    public int TotalPages { get; set; }
    
    [JsonProperty("number")]
    public int Number { get; set; }
}

public class BrregLinks
{
    [JsonProperty("first")]
    public BrregLink First { get; set; }
    
    [JsonProperty("prev")]
    public BrregLink Prev { get; set; }
    
    [JsonProperty("self")]
    public BrregLink Self { get; set; }
    
    [JsonProperty("next")]
    public BrregLink Next { get; set; }
    
    [JsonProperty("last")]
    public BrregLink Last { get; set; }
}

public class BrregLink
{
    [JsonProperty("href")]
    public string Href { get; set; }
}
