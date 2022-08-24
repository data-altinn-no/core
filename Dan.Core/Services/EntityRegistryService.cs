using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Newtonsoft.Json;
using Polly;
using Polly.Registry;
using System.Net;
using Dan.Core.Models;

namespace Dan.Core.Services;

class EntityRegistryService : IEntityRegistryService
{

    public static TimeSpan DistributedCacheTtl = TimeSpan.FromHours(12);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IPolicyRegistry<string> _policyRegistry;
    private readonly string[] _validUnitTypes = { "ADOS", "FKF", "FYLK", "KF", "KOMM", "ORGL", "STAT", "SF", "SÆR" };
    private readonly string[] _validSectorCodes = { "1110", "1120", "1510", "1520", "3900", "6100", "6500" };
    private const string CachingPolicy = "ERCachePolicy";
    private const string HttpClientName = "SafeHttpClient";

    public EntityRegistryService(IHttpClientFactory httpClientFactory, IPolicyRegistry<string> policyRegistry)
    {
        _httpClientFactory = httpClientFactory;
        _policyRegistry = policyRegistry;
    }

    /// <inheritdoc/>
    public async Task<BREntityRegisterEntry?> GetOrganizationEntry(string orgNumber)
    {
        if (Settings.IsDevEnvironment && Settings.TestEnvironmentValidOrgs.Contains(orgNumber))
        {
            return new BREntityRegisterEntry()
            {
                Organisasjonsnummer = Convert.ToInt32(orgNumber),
                Organisasjonsform = new Organisasjonsform { Kode = "STAT" },
                Navn = "TESTEORGANISASJON"
            };
        }

        // Since main and sub units are different resources, we try to find a main unit first, then fall back to sub unit
        var unit = await GetUnitFromBR(orgNumber, true)
                                ?? await GetUnitFromBR(orgNumber, false);

        return unit;
    }

    /// <inheritdoc/>
    public async Task<bool> IsOrganizationPublicAgency(string organizationNumber)
    {
        var entity = await GetOrganizationEntry(organizationNumber);

        return entity != null && IsPublicAgency(entity);
    }

    private bool IsPublicAgency(BREntityRegisterEntry entity)
    {
        return (!string.IsNullOrEmpty(entity.Organisasjonsform.Kode) && _validUnitTypes.Contains(entity.Organisasjonsform.Kode))
               || (!string.IsNullOrEmpty(entity.Naeringskode1.Kode) && entity.Naeringskode1.Kode.StartsWith("84"))
               || (!string.IsNullOrEmpty(entity.InstitusjonellSektorkode.Kode) && _validSectorCodes.Contains(entity.InstitusjonellSektorkode.Kode));
    }

    private bool IsSyntheticOrganizationNumber(string organizationNumber)
    {
        return organizationNumber.StartsWith("2") || organizationNumber.StartsWith("3");
    }

    private string GetValidationUrl(string organizationNumber)
    {
        var validationUrl = Settings.IsDevEnvironment && IsSyntheticOrganizationNumber(organizationNumber)
            ? Settings.SyntheticOrganizationValidationUrl
            : Settings.OrganizationValidationUrl;

        return string.Format(validationUrl, organizationNumber);
    }

    private string GetValidationUrlForSubUnits(string organizationNumber)
    {
        var validationUrl = Settings.IsDevEnvironment && IsSyntheticOrganizationNumber(organizationNumber)
            ? Settings.SyntheticOrganizationSubUnitValidationUrl
            : Settings.OrganizationSubUnitValidationUrl;

        return string.Format(validationUrl, organizationNumber);
    }

    // ReSharper disable once InconsistentNaming
    private async Task<BREntityRegisterEntry?> GetUnitFromBR(string organizationNumber, bool isMainUnit)
    {
        var validationUrl = isMainUnit
            ? GetValidationUrl(organizationNumber)
            : GetValidationUrlForSubUnits(organizationNumber);
        var request = new HttpRequestMessage(HttpMethod.Get, validationUrl);

        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.SetAllowedErrorCodes(HttpStatusCode.NotFound, HttpStatusCode.BadRequest);

        var cachePolicy = _policyRegistry.Get<AsyncPolicy<string?>>(CachingPolicy);
        var responseContent = await cachePolicy.ExecuteAsync(async _ => await GetResponseString(request),
            new Context(request.Key(CacheArea.Absolute)));

        BREntityRegisterEntry? result;
        if (responseContent == null) return null;
        try
        {
            result = JsonConvert.DeserializeObject<BREntityRegisterEntry>(responseContent);
        }
        catch
        {
            result = null;
        }

        return result;
    }

    private async Task<string?> GetResponseString(HttpRequestMessage request)
    {
        var client = _httpClientFactory.CreateClient(HttpClientName);
        HttpResponseMessage response = await client.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            return await response.Content.ReadAsStringAsync();
        }

        return null;
    }
}