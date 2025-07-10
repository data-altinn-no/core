using System.Configuration;
using Dan.Common.Exceptions;

namespace Dan.Common.Services;

public interface ICcrClientService
{
    Task<EntityRegistryUnit?> GetUnit(string organizationNumber, string env);
    Task<List<EntityRegistryUnit>?> GetSubunits(string organizationNumber, string env);
    Task<EntityRegistryUnit?> GetMainUnit(string organizationNumber, string env);
    Task<bool> IsPublic(string organizationNumber, string env);
    Task<EntityRegistryUnitHierarchy?> GetUnitHierarchy(string organizationNumber, string env);
}

public class CcrClientService(IHttpClientFactory httpClientFactory) : ICcrClientService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(Constants.SafeHttpClient);
    private const string CcrBaseLocal = "http://localhost:7071/api/ccr";
    private const string CrrBaseDev = "https://dev-api.data.altinn.no/v1/public/ccr";
    private const string CrrBaseTest = "https://test-api.data.altinn.no/v1/public/ccr";
    private const string CrrBaseProd = "https://api.data.altinn.no/v1/public/ccr";
    
    public async Task<EntityRegistryUnit?> GetUnit(string organizationNumber, string env)
    {
        var url = $"{GetBaseUrl(env)}/{organizationNumber}";
        return await GetCcrResponse<EntityRegistryUnit>(url);
    }

    public async Task<List<EntityRegistryUnit>?> GetSubunits(string organizationNumber, string env)
    {
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/subunits";
        return await GetCcrResponse<List<EntityRegistryUnit>>(url);
    }

    public async Task<EntityRegistryUnit?> GetMainUnit(string organizationNumber, string env)
    {
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/mainunit";
        return await GetCcrResponse<EntityRegistryUnit>(url);
    }

    public async Task<bool> IsPublic(string organizationNumber, string env)
    {
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/ispublic";
        return await GetCcrResponse<bool>(url);
    }

    public async Task<EntityRegistryUnitHierarchy?> GetUnitHierarchy(string organizationNumber, string env)
    {
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/hierarchy";
        return await GetCcrResponse<EntityRegistryUnitHierarchy>(url);
    }

    private async Task<T?> GetCcrResponse<T>(string url)
    {
        var response = await httpClient.GetAsync(url);
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
            {
                var responseString = await response.Content.ReadAsStringAsync();
                var unit = JsonConvert.DeserializeObject<T>(responseString);
                return unit;
            }
            case HttpStatusCode.NotFound:
                return default;
            case HttpStatusCode.NoContent:
                throw new CcrException(ErrorCode.UpstreamException, "Unexpected HTTP status code from CCR, no content found");
            case HttpStatusCode.BadRequest:
                throw new CcrException(ErrorCode.UpstreamException, "Bad request");
            case HttpStatusCode.TooManyRequests:
                throw new CcrException(ErrorCode.QuotaExceededException, "Too many requests to CCR, Try again later.");
            default:
                throw new CcrException(ErrorCode.UpstreamException, "Unexpected HTTP status code from external API");
        }
    }
    
    // TODO: Add synth checks
    private bool IsSyntheticOrganizationNumber(string organizationNumber)
    {
        return organizationNumber.StartsWith('2') || organizationNumber.StartsWith('3');
    }

    private static string GetBaseUrl(string env)
    {
        if (string.IsNullOrWhiteSpace(env))
        {
            throw new ArgumentNullException(nameof(env), "Environment cannot be null or empty");
        }
        
        env = env.ToLower().Trim();
        return env switch
        {
            "local" => CcrBaseLocal,
            "dev" => CrrBaseDev,
            "test" => CrrBaseTest,
            "prod" => CrrBaseProd,
            _ => throw new ArgumentOutOfRangeException(nameof(env), env, $"Invalid ccr environment: {env}, must be local, dev, test or prod")
        };
    }
}