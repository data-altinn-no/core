using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using Dan.Core.Models;

namespace Dan.Core.Services;

/// <summary>
/// Helper class for requests to Altinn service owner REST api
/// </summary>
public class AltinnServiceOwnerApiService : IAltinnServiceOwnerApiService
{
    private readonly ILogger<AltinnServiceOwnerApiService> _log;
    private const string RoleUrl = "{0}authorization/roles?ForceEIAuthentication&subject={1}&reportee={2}&language=1033";
    private const string RightsUrl = "{0}reportees?ForceEIAuthentication=true&subject={1}&ServiceCode={2}&ServiceEdition={3}";
    private const string RoleMetaUrl = "{0}roledefinitions?ForceEIAuthentication&language=1033";
    private const string GetSrrUrl = "{0}Srr?ForceEIAuthentication=true&reportee={1}&serviceCode={2}&serviceEditionCode={3}";
    private const string PostSrrUrl = "{0}Srr?ForceEIAuthentication=true";
    private const string DeleteSrrUrl = "{0}Srr/{1}?ForceEIAuthentication=true";
    private const string OrganizationsUrl = "{0}organizations/{1}?ForceEIAuthentication=true";
    private const string LanguageEn = "1033";
    private readonly string _baseUrl;
    private readonly HttpClient _client;

    /// <summary>
    /// Create a new AltinnServiceOwnerHelper instance
    /// </summary>                
    /// <param name="httpClientFactory"></param>
    /// <param name="loggerFactory"></param>     
    public AltinnServiceOwnerApiService(IHttpClientFactory httpClientFactory, ILoggerFactory loggerFactory)
    {
        _log = loggerFactory.CreateLogger<AltinnServiceOwnerApiService>();
        _client = httpClientFactory.CreateClient("ECHttpClient");
        _baseUrl = Settings.AltinnServiceOwnerApiUri;
    }

    /// <summary>
    /// Validate an Altinn role delegation between two parties
    /// </summary>                
    /// <param name="offeredby">
    /// The ssn/orgno of the delegator of the role
    /// </param>
    /// <param name="coveredby">
    /// The ssn/orgno of the receiver of the role
    /// </param>
    /// <param name="roleCode">
    /// The name of the Altinn role
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    public async Task<bool> VerifyAltinnRole(string offeredby, string coveredby, string roleCode)
    {
        int? id = await GetAltinnRoleDefinitionId(roleCode);

        if (id != null && id > 0)
        {
            var response = await MakeRequest(string.Format(RoleUrl, _baseUrl, coveredby, offeredby));
            var list = JsonConvert.DeserializeObject<List<AltinnRoleDefinition>>(response);
            return list.FindIndex(x => x.RoleDefinitionId == id) >= 0;
        }
        else
        {
            return false;
        }
    }

    /// <summary>
    /// Verify that coveredby has rights to the service on behalf of offeredby
    /// </summary>                
    /// <param name="offeredby">
    /// The ssn/orgno of the delegator of the role
    /// </param>
    /// <param name="coveredby">
    /// The ssn/orgno of the receiver of the role
    /// </param>
    /// <param name="serviceCode">
    /// The service code identifier of an Altinn service
    /// </param>
    /// <param name="serviceEdition">
    /// The service edition identifier of an Altinn service
    /// </param>
    /// <returns>
    /// True if validated, false if invalid
    /// </returns>
    public async Task<bool> VerifyAltinnRight(string offeredby, string coveredby, string serviceCode, string serviceEdition)
    {
        var response = await MakeRequest(string.Format(RightsUrl, _baseUrl, coveredby, serviceCode, serviceEdition));
        return response.Contains(offeredby);
    }

    /// <summary>
    /// Makes sure a SRR right exists for the reportee and handledby for all required services
    /// </summary>
    /// <param name="requestor">The reportee in SRR</param>
    /// <param name="validTo">How long the rule should be valid</param>
    /// <param name="evidenceCodesRequiringConsent">List of ECs requiring SRR rules</param>
    /// <returns></returns>
    public async Task EnsureSrrRights(string requestor, DateTime validTo, IEnumerable<EvidenceCode> evidenceCodesRequiringConsent)
    {

        // Filter out evidenceCodes with ConsentRequirement having RequiresSrr=false. 
        var evidenceCodesRequiringConsentForSrr = evidenceCodesRequiringConsent.Where(ec =>
        {
            if (ec.AuthorizationRequirements == null || !ec.AuthorizationRequirements.OfType<ConsentRequirement>().Any())
            {
                // Assume always SRR on consent-service evidence codes without requirements
                return true;
            }

            return ec.AuthorizationRequirements.OfType<ConsentRequirement>().First()!.RequiresSrr;
        });


        await DeleteExistingSrrRights(requestor, evidenceCodesRequiringConsent);
        await AddSrrRights(requestor, validTo, evidenceCodesRequiringConsent);
    }

    /// <summary>
    /// Looks up a organization by its organization number
    /// </summary>
    /// <param name="orgNumber">Organization number</param>
    /// <returns>The organization</returns>
    public async Task<Organization> GetOrganization(string orgNumber)
    {
        var result = await MakeRequest(string.Format(OrganizationsUrl, _baseUrl, orgNumber));
        return JsonConvert.DeserializeObject<Organization>(result);
    }

    private async Task DeleteExistingSrrRights(string requestor, IEnumerable<EvidenceCode> evidenceCodesRequiringConsent)
    {
        foreach (var ec in evidenceCodesRequiringConsent)
        {
            (string serviceCode, int serviceEditionCode) = GetServiceCodeAndEditionFromEvidenceCode(ec);
            var result = await MakeRequest(string.Format(GetSrrUrl, _baseUrl, requestor, serviceCode, serviceEditionCode));
            var rights = JsonConvert.DeserializeObject<List<SrrRight>>(result);
            foreach (var right in rights)
            {
                // Delete the existing to avoid conflicts
                _log.LogInformation("Deleting existing SRR right for reportee={requestor} for sc={serviceCode} sec={serviceEditionCode}", requestor, serviceCode, serviceEditionCode);

                var request = new HttpRequestMessage(HttpMethod.Delete, string.Format(DeleteSrrUrl, _baseUrl, right.Id));
                request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnServiceOwnerApiKey);
                request.Headers.TryAddWithoutValidation("Accept", "application/json");

                var deleteResult = await _client.SendAsync(request);
                if (!deleteResult.IsSuccessStatusCode)
                {
                    throw new HttpRequestException(deleteResult.ReasonPhrase);
                }
            }
        }
    }

    private async Task AddSrrRights(string requestor, DateTime validTo, IEnumerable<EvidenceCode> evidenceCodesRequiringConsent)
    {
        var condition = new SrrRightCondition(Settings.SrrRightsCondition);

        foreach (var ec in evidenceCodesRequiringConsent)
        {
            (string serviceCode, int serviceEditionCode) = GetServiceCodeAndEditionFromEvidenceCode(ec);
            var srrRight = new SrrRight()
            {
                ServiceCode = serviceCode,
                ServiceEditionCode = serviceEditionCode,
                Condition = condition,
                Reportee = requestor,
                ValidTo = validTo,
                Right = "Read"
            };


            var request = new HttpRequestMessage(HttpMethod.Post, string.Format(PostSrrUrl, _baseUrl));
            request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnServiceOwnerApiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Content = new StringContent(JsonConvert.SerializeObject(new List<SrrRight>() { srrRight }), Encoding.UTF8, "application/hal+json");

            _log.LogInformation("Adding SRR right for reportee={requestor}, handledby={handledBy} for sc={serviceCode} sec={serviceEditionCode}", requestor, condition.HandledBy, serviceCode, serviceEditionCode);
            var result = await _client.SendAsync(request);
            if (!result.IsSuccessStatusCode)
            {
                throw new HttpRequestException(result.ReasonPhrase);
            }
        }
    }

    private (string, int) GetServiceCodeAndEditionFromEvidenceCode(EvidenceCode ec)
    {
        if (!string.IsNullOrEmpty(ec.ServiceCode) && ec.ServiceEditionCode > 0)
        {
            return (ec.ServiceCode, ec.ServiceEditionCode);
        }

        if (ec.AuthorizationRequirements == null || !ec.AuthorizationRequirements.OfType<ConsentRequirement>().Any())
        {
            throw new InternalServerErrorException(
                $"Unable to determine service code / edition on evidence code {ec.EvidenceCodeName}");
        }

        // We assume that we only have a single ConsentRequirement for each service context. At this point, the evidence code should only 
        // contain the requirements that apply to the active request context
        var req = ec.AuthorizationRequirements.OfType<ConsentRequirement>().First()!;

        return (req.ServiceCode, req.ServiceEdition);
    }

    private async Task<int?> GetAltinnRoleDefinitionId(string roleCode)
    {
        var target = string.Format(RoleMetaUrl, _baseUrl, LanguageEn);
        var response = await MakeRequest(target);

        List<AltinnRoleDefinition> list = JsonConvert.DeserializeObject<List<AltinnRoleDefinition>>(response);
        int? resultId = list.Find(x => x.RoleDefinitionCode == roleCode)?.RoleDefinitionId;

        return resultId;
    }

    private async Task<string> MakeRequest(string target)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, target);
        request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnServiceOwnerApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.SetAllowedErrorCodes(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        try
        {
            var result = await _client.SendAsync(request);
            return await result.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException || ex is TimeoutException)
            {
                throw new ServiceNotAvailableException("Failed to check Altinn's service owner api", ex);
            }

            throw;

        }
    }
}
