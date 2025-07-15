using Dan.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Dan.Common.Services;

/// <summary>
/// Service to fetch entity registry units and information from Central Coordination Registry
/// </summary>
public interface ICcrClientService
{
    /// <summary>
    /// Flag to set if allowed to look up synthetic users
    /// </summary>
    public bool AllowTestCcrLookup { get; set; }
    
    /// <summary>
    /// Fetches Entity Registry Unit, looks up main unit first, attempts to look up subunit if main unit not found
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    Task<EntityRegistryUnit?> GetUnit(string organizationNumber, string env);
    
    /// <summary>
    /// Fetches subunits of provided organisation
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch subunits of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    Task<List<EntityRegistryUnit>?> GetSubunits(string organizationNumber, string env);
    
    /// <summary>
    /// Fetches main unit of provided organisation
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch main unit of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    Task<EntityRegistryUnit?> GetMainUnit(string organizationNumber, string env);
    
    /// <summary>
    /// Checks if an entity registry unit is a public agency
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to check</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    Task<bool> IsPublic(string organizationNumber, string env);
    
    /// <summary>
    /// Gets full unit hierarchy of organisation. Hierarchy is unit information, subunits of unit
    /// and recursively subunits of subunits.
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch hierarchy of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    Task<EntityRegistryUnitHierarchy?> GetUnitHierarchy(string organizationNumber, string env);
}

/// <summary>
/// Service to fetch entity registry units and information from Central Coordination Registry
/// </summary>
public class CcrClientService(IHttpClientFactory httpClientFactory, ILogger<CcrClientService> logger) : ICcrClientService
{
    private readonly HttpClient httpClient = httpClientFactory.CreateClient(Constants.SafeHttpClient);
    private const string CcrBaseLocal = "http://localhost:7071/api/ccr";
    private const string CrrBaseDev = "https://dev-api.data.altinn.no/v1/public/ccr";
    private const string CrrBaseTest = "https://test-api.data.altinn.no/v1/public/ccr";
    private const string CrrBaseProd = "https://api.data.altinn.no/v1/public/ccr";
    
    /// <summary>
    /// Flag to set if allowed to look up synthetic users
    /// </summary>
    public bool AllowTestCcrLookup { get; set; } = false;
    
    /// <summary>
    /// Fetches Entity Registry Unit, looks up main unit first, attempts to look up subunit if main unit not found
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    public async Task<EntityRegistryUnit?> GetUnit(string organizationNumber, string env)
    {
        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return null;
        }
        var url = $"{GetBaseUrl(env)}/{organizationNumber}";
        return await GetCcrResponse<EntityRegistryUnit>(url);
    }

    /// <summary>
    /// Fetches subunits of provided organisation
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch subunits of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    public async Task<List<EntityRegistryUnit>?> GetSubunits(string organizationNumber, string env)
    {
        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return null;
        }
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/subunits";
        return await GetCcrResponse<List<EntityRegistryUnit>>(url);
    }

    /// <summary>
    /// Fetches main unit of provided organisation
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch main unit of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    public async Task<EntityRegistryUnit?> GetMainUnit(string organizationNumber, string env)
    {
        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return null;
        }
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/mainunit";
        return await GetCcrResponse<EntityRegistryUnit>(url);
    }

    /// <summary>
    /// Checks if an entity registry unit is a public agency
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to check</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    public async Task<bool> IsPublic(string organizationNumber, string env)
    {
        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return false;
        }
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/ispublic";
        return await GetCcrResponse<bool>(url);
    }

    /// <summary>
    /// Gets full unit hierarchy of organisation. Hierarchy is unit information, subunits of unit
    /// and recursively subunits of subunits.
    /// </summary>
    /// <param name="organizationNumber">Organisation number of unit to fetch hierarchy of</param>
    /// <param name="env">Environment to fetch from: local, dev, test, prod</param>
    public async Task<EntityRegistryUnitHierarchy?> GetUnitHierarchy(string organizationNumber, string env)
    {
        if (IsSyntheticOrganizationNumber(organizationNumber) && !AllowTestCcrLookup)
        {
            return null;
        }
        var url = $"{GetBaseUrl(env)}/{organizationNumber}/hierarchy";
        return await GetCcrResponse<EntityRegistryUnitHierarchy>(url);
    }

    private async Task<T?> GetCcrResponse<T>(string url)
    {
        HttpResponseMessage response;
        try
        {
            response = await httpClient.GetAsync(url);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to get CCR response: {message}", e.Message);
            throw;
        }
        switch (response.StatusCode)
        {
            case HttpStatusCode.OK:
            {
                try
                {
                    var responseString = await response.Content.ReadAsStringAsync();
                    var unit = JsonConvert.DeserializeObject<T>(responseString);
                    return unit;
                }
                catch (Exception e)
                {
                    logger.LogError(e, "Failed to deserialize CCR response: {message}", e.Message);
                    throw;
                }
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
    
    private static bool IsSyntheticOrganizationNumber(string organizationNumber)
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