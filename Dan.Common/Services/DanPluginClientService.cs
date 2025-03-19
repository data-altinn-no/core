using System.Text;
using Dan.Common.Exceptions;

namespace Dan.Common.Services;

/// <summary>
/// Service for calling DAN plugins
/// </summary>
public interface IDanPluginClientService
{
    /// <summary>
    /// Get dataset value
    /// </summary>
    public Task<T?> GetPluginDataSetAsync<T>(EvidenceHarvesterRequest request, string code, string env, bool isDefaultJson, string url = "", string source = "") where T: new();
}

/// <summary>
/// Service for calling DAN plugins
/// </summary>
public class DanPluginClientService : IDanPluginClientService
{
    private readonly HttpClient _httpClient;
    private const string MetadataDev = "https://dev-api.data.altinn.no/v1/public/metadata/evidencecodes";
    private const string MetadataTest = "https://test-api.data.altinn.no/v1/public/metadata/evidencecodes";
    private const string MetadataProd = "https://api.data.altinn.no/v1/public/metadata/evidencecodes";
    private const string PluginDev = "https://func-es{0}-test-dev.azurewebsites.net/api/{1}?code={2}";
    private const string PluginTest = "https://func-es{0}-prod-prod-staging.azurewebsites.net/api/{1}?code={2}";
    private const string PluginProd = "https://func-es{0}-prod-prod.azurewebsites.net/api/{1}?code={2}";

    /// <summary>
    /// Sets up service with a safe http client built from provided factory
    /// </summary>
    public DanPluginClientService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient(Constants.PluginHttpClient);
    }
    
    /// <summary>
    /// Get dataset value
    /// </summary>
    public async Task<T?> GetPluginDataSetAsync<T>(EvidenceHarvesterRequest request, string code, string env, bool isDefaultJson, string pluginUrl = "", string source = "") where T : new()
    {
        HttpResponseMessage? response = default;
        var result = default(T);
        try
        {
            var url = env switch
                {
                    "dev" => MetadataDev,
                    "test" => MetadataTest,
                    "prod" => MetadataProd,
                    _ => throw new ArgumentOutOfRangeException(nameof(env), "env must be dev, test or prod"),
                };
            
            var metadataResponse = await _httpClient.GetAsync(url);
            
            EvidenceCode? evidenceCode;
            if (metadataResponse.IsSuccessStatusCode)
            {
                var evidenceCodes = JsonConvert.DeserializeObject<List<EvidenceCode>>(await metadataResponse.Content.ReadAsStringAsync());
                evidenceCode = evidenceCodes?.FirstOrDefault(x => x.EvidenceCodeName == request.EvidenceCodeName);
                if (evidenceCode == null)
                {
                    throw new EvidenceSourceTransientException(1, "Dataset not found");
                }
                if (!string.IsNullOrEmpty(evidenceCode.RequiredScopes))
                {
                    throw new ArgumentOutOfRangeException(nameof(request.MPToken), $"Dataset requires maskinporten token which was not supplied");
                }
            }
            else
            {
                throw new EvidenceSourceTransientException(1, "Dataset not found");
            }
            pluginUrl = string.IsNullOrEmpty(pluginUrl) ? GetPluginUrl(env, source, evidenceCode.EvidenceCodeName, code) : pluginUrl;
            response = await _httpClient.PostAsync(pluginUrl, new StringContent(JsonConvert.SerializeObject(request),Encoding.UTF8, "application/json"));
            switch (response.StatusCode)
            {
                case HttpStatusCode.OK:
                {
                    var content = await response.Content.ReadAsStringAsync();
                    if (isDefaultJson)
                    {
                        var evidenceValues = JsonConvert.DeserializeObject<List<EvidenceValue>>(content);
                        var evidenceValue = evidenceValues?.First(x => x.EvidenceValueName == "default");
                        if (evidenceValue?.Value is null)
                        {
                            return result;
                        }

                        var stringvalue = evidenceValue.Value.ToString();
                        result = JsonConvert.DeserializeObject<T>(stringvalue!);
                    }
                    else
                    {
                        result = JsonConvert.DeserializeObject<T>(content);
                    }
                }
                    break;
                case HttpStatusCode.NoContent:
                    throw new EvidenceSourcePermanentClientException((int)ErrorCode.UpstreamException, "Unexpected HTTP status code from external API, no content found");
                case HttpStatusCode.BadRequest:
                    throw new EvidenceSourcePermanentClientException((int)ErrorCode.UpstreamException, "Bad request");
                case HttpStatusCode.UnprocessableEntity:
                    throw new EvidenceSourceTransientException((int)ErrorCode.QuotaExceededException, "Quota exceeded. Try again tomorrow.");
                default:
                    throw new EvidenceSourcePermanentClientException((int)ErrorCode.UpstreamException, "Unexpected HTTP status code from external API");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new EvidenceSourcePermanentServerException((int)ErrorCode.UpstreamException, null, ex);
        }
        finally
        {
            response?.Dispose();
        }
        return result;
    }
    
    private static string GetPluginUrl(string env, string source, string evidenceCodeName, string code)
    {
        var result = env switch
        {
            "dev" => string.Format(PluginDev, source, evidenceCodeName, code),
            "test" => string.Format(PluginTest, source, evidenceCodeName, code),
            "prod" => string.Format(PluginProd, source, evidenceCodeName, code),
            _ => throw new ArgumentOutOfRangeException(nameof(env), $"env must be dev, test or prod"),
        };
        return result;
    }
}