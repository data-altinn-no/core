using AsyncKeyedLock;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace Dan.Core.Services;

/// <summary>
/// Helper class for requests to Altinn service owner REST api
/// </summary>
public class AltinnServiceOwnerApiService : IAltinnServiceOwnerApiService
{
    private readonly ILogger<AltinnServiceOwnerApiService> _log;
    private const string RoleUrl = "{0}authorization/roles?ForceEIAuthentication&subject={1}&reportee={2}&language=1033";
    private const string RightsUrl = "{0}reportees?ForceEIAuthentication=true&subject={1}&ServiceCode={2}&ServiceEdition={3}";
    private const string RoleMetaUrl = "{0}roledefinitions?ForceEIAuthentication&language={1}";
    private const string GetSrrUrl = "{0}Srr?ForceEIAuthentication=true&reportee={1}&serviceCode={2}&serviceEditionCode={3}";
    private const string PostSrrUrl = "{0}Srr?ForceEIAuthentication=true";
    private const string PutSrrUrl = "{0}Srr/{1}?ForceEIAuthentication=true";
    private const string OrganizationsUrl = "{0}organizations/{1}?ForceEIAuthentication=true";
    private const string LanguageEn = "1033";
    private readonly string _baseUrl;
    private readonly HttpClient _client;
    private readonly AsyncKeyedLocker<string> _asyncKeyedLock = new(o => o.PoolSize = 10);

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
        var id = await GetAltinnRoleDefinitionId(roleCode);

        if (id is not > 0) return false;

        var response = await MakeRequest(string.Format(RoleUrl, _baseUrl, coveredby, offeredby));
        var list = JsonConvert.DeserializeObject<List<AltinnRoleDefinition>>(response) ?? new List<AltinnRoleDefinition>();
        return list.FindIndex(x => x.RoleDefinitionId == id) >= 0;
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
            if (!ec.AuthorizationRequirements.OfType<ConsentRequirement>().Any())
            {
                // Assume always SRR on consent-service evidence codes without requirements
                return true;
            }

            return ec.AuthorizationRequirements.OfType<ConsentRequirement>().First().RequiresSrr;
        }).ToList();


        await UpdateExistingSrrRights(requestor, evidenceCodesRequiringConsentForSrr, validTo);
    }

    /// <summary>
    /// Looks up a organization by its organization number
    /// </summary>
    /// <param name="orgNumber">Organization number</param>
    /// <returns>The organization</returns>
    public async Task<Organization?> GetOrganization(string orgNumber)
    {
        var result = await MakeRequest(string.Format(OrganizationsUrl, _baseUrl, orgNumber));
        return JsonConvert.DeserializeObject<Organization>(result);
    }

    private async Task UpdateExistingSrrRights(string requestor, IEnumerable<EvidenceCode> evidenceCodesRequiringConsent, DateTime validTo)
    {
        var tasks = new List<Task>();

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

            tasks.Add(UpdateSrrRights(srrRight));
        }

        await Task.WhenAll(tasks);
    }

    private async Task UpdateSrrRights(SrrRight singleRight)
    {
        var key = $"{singleRight.Reportee}_{singleRight.ServiceCode}_{singleRight.ServiceEditionCode}";

        using (await _asyncKeyedLock.LockAsync(key))
        {
            var result = await MakeRequest(string.Format(GetSrrUrl, _baseUrl, singleRight.Reportee, singleRight.ServiceCode, singleRight.ServiceEditionCode), HttpMethod.Get);
            var rights = JsonConvert.DeserializeObject<List<SrrRight>>(result);

            if (rights == null)
            {
                throw new ServiceNotAvailableException("Invalid return from Altinn SRR");
            }

            if (rights.Count == 0)
            {
                await AddSrrRight(singleRight.Reportee, singleRight);
            }
            else
            {
                foreach (var right in rights)
                {
                    //Set datetime further forward, and update condition in case it is changed (or environment specific)
                    right.ValidTo = singleRight.ValidTo;
                    right.Condition = singleRight.Condition;
                    await UpdateSrrRight(right);
                }
            }
        }
    }

    private async Task UpdateSrrRight(SrrRight right)
    {
        // Update existing to avoid conflicts
        _log.LogInformation("Updating existing SRR right for reportee={requestor} for sc={serviceCode} sec={serviceEditionCode}",
            right.Reportee, right.ServiceCode, right.ServiceEditionCode);

        await MakeRequest(string.Format(PutSrrUrl, _baseUrl, right.Id), HttpMethod.Put, new StringContent(
            JsonConvert.SerializeObject(right), Encoding.UTF8,
            "application/hal+json"));
    }

    private async Task AddSrrRight(string requestor, SrrRight right)
    {
        var condition = new SrrRightCondition(Settings.SrrRightsCondition);
        _log.LogInformation("Adding SRR right for reportee={requestor}, handledby={handledBy} for sc={serviceCode} sec={serviceEditionCode}", requestor, condition.HandledBy, right.ServiceCode, right.ServiceEditionCode);

        await MakeRequest(string.Format(PostSrrUrl, _baseUrl), HttpMethod.Post, new StringContent(JsonConvert.SerializeObject(new List<SrrRight>() { right }), Encoding.UTF8, "application/hal+json"));
    }

    private (string, int) GetServiceCodeAndEditionFromEvidenceCode(EvidenceCode ec)
    {
        // Legacy handling
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
        var req = ec.AuthorizationRequirements.OfType<ConsentRequirement>().First();

        return (req.ServiceCode, req.ServiceEdition);
    }

    private async Task<int?> GetAltinnRoleDefinitionId(string roleCode)
    {
        var target = string.Format(RoleMetaUrl, _baseUrl, LanguageEn);
        var response = await MakeRequest(target);

        List<AltinnRoleDefinition> list = JsonConvert.DeserializeObject<List<AltinnRoleDefinition>>(response) ?? new List<AltinnRoleDefinition>();
        int? resultId = list.Find(x => x.RoleDefinitionCode == roleCode)?.RoleDefinitionId;

        return resultId;
    }

    private async Task<string> MakeRequest(string target, HttpMethod? method = null, HttpContent? body = null)
    {
        var request = new HttpRequestMessage(method ?? HttpMethod.Get, target);
        request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnServiceOwnerApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.SetAllowedErrorCodes(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        if (body != null)
        {
            request.Content = body;
        }

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
