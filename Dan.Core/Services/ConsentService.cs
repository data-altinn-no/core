using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;
using Dan.Core.Models;

namespace Dan.Core.Services;

/// <summary>
/// Class containing methods for initiating and checking consents
/// </summary>
public class ConsentService : IConsentService
{
    /// <summary>
    /// Magic string used as authorization code when the user has actively denied a consent request
    /// </summary>
    public const string ConsentDenied = "denied";

    /// <summary>
    /// Magic string for claim name for validTo
    /// </summary>
    public const string ConsentJwtValidtoKey = "ValidToDate";

    private readonly HttpClient _httpClient;
    private readonly HttpClient _noCertHttpClient;
    private readonly ILogger<ConsentService> _logger;
    private readonly IAltinnCorrespondenceService _correspondenceService;
    private readonly IEntityRegistryService _entityRegistryService;
    private readonly IAltinnServiceOwnerApiService _altinnServiceOwnerApiService;
    private readonly IRequestContextService _requestContextService;

    /// <summary>
    /// Constructor for Consent helper
    /// </summary>
    /// <param name="loggerFactory"></param>
    /// <param name="httpClientFactory"></param>
    /// <param name="altinnCorrespondenceService"></param>
    /// <param name="entityRegistryService"></param>
    /// <param name="altinnServiceOwnerApiService"></param>
    /// <param name="availableEvidenceCodesService"></param>
    /// <param name="requestContextService"></param>
    public ConsentService(
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory,
        IAltinnCorrespondenceService altinnCorrespondenceService,
        IEntityRegistryService entityRegistryService,
        IAltinnServiceOwnerApiService altinnServiceOwnerApiService,
        IAvailableEvidenceCodesService availableEvidenceCodesService,
        IRequestContextService requestContextService)
    {
        _httpClient = httpClientFactory.CreateClient("ECHttpClient");
        _noCertHttpClient = httpClientFactory.CreateClient("SafeHttpClient");

        _logger = loggerFactory.CreateLogger<ConsentService>();
        _correspondenceService = altinnCorrespondenceService;
        _entityRegistryService = entityRegistryService;
        _altinnServiceOwnerApiService = altinnServiceOwnerApiService;
        _requestContextService = requestContextService;

        _entityRegistryService.UseCoreProxy = false;
        _entityRegistryService.AllowTestCcrLookup = !Settings.IsProductionEnvironment;
    }

    /// <summary>
    /// Initiating a consent based on the passed (validated) accreditation
    /// </summary>
    /// <param name="accreditation">
    /// The accreditation.
    /// </param>
    /// <param name="skipAltinnNotification"></param>
    /// <returns>
    /// The <see cref="Task"/>.
    /// </returns>
    public async Task Initiate(Accreditation accreditation, bool skipAltinnNotification)
    {
        var evidenceCodesRequiringConsent = GetEvidenceCodesRequiringConsentForActiveContext(accreditation);
        if (evidenceCodesRequiringConsent.Count == 0)
        {
            throw new ArgumentException("Expected at least one evidencecode in the accreditation requiring consent");
        }

        if (accreditation.Requestor == null)
        {
            throw new InvalidRequestorException("Requestor was null for consent initiation");
        }

        if (accreditation.Subject == null)
        {
            throw new InvalidSubjectException("Subject was null for consent initiation");
        }

        var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
        var subjectName = await GetPartyDisplayName(accreditation.SubjectParty);

        await _altinnServiceOwnerApiService.EnsureSrrRights(accreditation.Requestor, accreditation.ValidTo.AddYears(10), evidenceCodesRequiringConsent);

        if (_requestContextService.ServiceContext.ServiceContextTextTemplate == null)
        {
            throw new ServiceContextException(
                "ServiceContextTemplate was not defined, required for consent initiation");
        }

        var processedConsentRequestStrings = TextTemplateProcessor.ProcessConsentRequestMacros(_requestContextService.ServiceContext.ServiceContextTextTemplate.ConsentDelegationContexts, accreditation, requestorName, subjectName, _requestContextService.ServiceContext.Name);
        var consentRequest = await CreateConsentRequest(accreditation, evidenceCodesRequiringConsent, processedConsentRequestStrings);

        _logger.DanLog(accreditation, LogAction.ConsentRequested);
        foreach (var ec in evidenceCodesRequiringConsent)
        {
           _logger.DanLog(accreditation, LogAction.DatasetRequiringConsentRequested, ec.EvidenceCodeName);
        }

        accreditation.AltinnConsentUrl = GetConsentRequestUrl(consentRequest);

        var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_requestContextService.ServiceContext, accreditation, requestorName, subjectName, accreditation.AltinnConsentUrl);

        if (!skipAltinnNotification)
        {
            _logger.DanLog(accreditation, LogAction.CorrespondenceSent);
            await SendCorrespondence(accreditation, renderedTexts);
        }
    }

    /// <summary>
    /// The check.
    /// </summary>
    /// <param name="accreditation">
    /// The accreditation.
    /// </param>
    /// <param name="onlyLocalCheck">
    /// Whether or not to skip the call to Altinn API to get the token to check for revocation/expiration
    /// </param>
    /// <returns>
    /// The <see cref="ConsentStatus"/>.
    /// </returns>
    public async Task<ConsentStatus> Check(Accreditation accreditation, bool onlyLocalCheck = false)
    {
        var evidenceCodesRequiringConsent = GetEvidenceCodesRequiringConsentForActiveContext(accreditation);
        if (evidenceCodesRequiringConsent.Count == 0)
        {
            _logger.LogError("Expected at least one evidencecode in the accreditation requiring consent in accredition {accreditationId}", accreditation.AccreditationId);
        }

        if (accreditation.AuthorizationCode == null)
        {
            return ConsentStatus.Pending;
        }

        if (accreditation.AuthorizationCode == ConsentDenied)
        {
            return ConsentStatus.Denied;
        }

        if (accreditation.ValidTo < DateTime.Now)
        {
            return ConsentStatus.Expired;
        }

        if (!onlyLocalCheck)
        {
            var claims = await GetClaims(accreditation);

            if (claims == null)
            {
                return accreditation.ValidTo < DateTime.Now ? ConsentStatus.Expired : ConsentStatus.Revoked;
            }

            // Probably not needed; Altinn will not return an expired token
            var secSince1970 = Convert.ToInt64(claims.FindFirst(ConsentJwtValidtoKey)?.Value);
            var validTo = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(secSince1970);
            if (validTo < DateTime.UtcNow)
            {
                return ConsentStatus.Expired;
            }
        }

        return ConsentStatus.Granted;
    }

    /// <summary>
    /// Uses Altinn API to get a JWT token for the authorization code in the accreditation
    /// </summary>
    /// <param name="accreditation">Accreditation containing an authorization code</param>
    /// <returns>A JWT</returns>
    public async Task<string> GetJwt(Accreditation accreditation)
    {
        if (accreditation.AuthorizationCode is null or ConsentDenied)
        {
            throw new RequiresConsentException("The accreditation is missing a valid authorization code for the consent");
        }

        var url = Settings.GetConsentStatusUrl(accreditation.AuthorizationCode);

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.SetAllowedErrorCodes(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);
        _logger.LogInformation("Getting jwt: " + url);
        try
        {
            var result = await _noCertHttpClient.SendAsync(request);

            _logger.LogInformation($"JWT-get {result.RequestMessage} result: {result.StatusCode} : {result.ReasonPhrase}");

            // Altinn returns JWTs as bare JSON strings (with leading and trailing double quotes) 
            var jwt = await result.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(jwt))
            {
                throw new ServiceNotAvailableException("Unable to parse consent token from Altinn");
            }

            return jwt.Replace('"', ' ').Trim();
        }
        catch (Exception ex)
        {
            if (ex is HttpRequestException || ex is TimeoutException)
            {
                throw new ServiceNotAvailableException($"Failed to check consent status for AccreditationId={accreditation.AccreditationId}, Subject={accreditation.Subject}", ex);
            }

            throw;
        }
    }

    public bool EvidenceCodeRequiresConsent(EvidenceCode evidenceCode)
    {
        if (!evidenceCode.AuthorizationRequirements.OfType<ConsentRequirement>().Any())
        {
            return false;
        }

        return evidenceCode.AuthorizationRequirements.OfType<ConsentRequirement>().Any(x =>
            x.AppliesToServiceContext.Count == 0 || x.AppliesToServiceContext.Contains(_requestContextService.ServiceContext.Name));
    }

    /// <summary>
    /// Uses Altinn API to log the use of a consent
    /// </summary>
    /// <param name="accreditation">Accreditation containing an authorization code</param>
    /// <param name="evidence">The evidenceCode</param>
    /// <param name="dateTime">The dateTime when usage was done</param>
    /// <returns>Success status</returns>
    public async Task<bool> LogUse(Accreditation accreditation, EvidenceCode evidence, DateTime? dateTime = null)
    {
        if (accreditation.AuthorizationCode is null or ConsentDenied)
        {
            throw new RequiresConsentException("The accreditation is missing a valid authorization code for the consent");
        }

        dateTime ??= DateTime.Now;

        var url = Settings.GetConsentLoggingUrl(accreditation.AuthorizationCode);

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.SetAllowedErrorCodes(HttpStatusCode.BadRequest, HttpStatusCode.Forbidden);

        (string serviceCode, int serviceEdtionCode) = GetServiceCodeAndEditionFromEvidenceCode(evidence);

        request.JsonContent(new LogUseModel()
        {
            ServiceCode = serviceCode,
            ServiceEditionCode = serviceEdtionCode,
            UsageDateTime = dateTime.Value.ToString("o")
        });

        try
        {
            var result = await _noCertHttpClient.SendAsync(request);
            return result.StatusCode == HttpStatusCode.NoContent;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Failed to  log consent used (Altinn) for AccreditationId={accreditation.AccreditationId}, Subject={accreditation.Subject}:\n{ex}");
            return false;
        }
    }

    public List<EvidenceCode> GetEvidenceCodesRequiringConsentForActiveContext(Accreditation accreditation)
    {
        return accreditation.EvidenceCodes.Where(EvidenceCodeRequiresConsent).ToList();
    }

    private async Task<ClaimsIdentity?> GetClaims(Accreditation accreditation)
    {
        var jwt = await GetJwt(accreditation);

        if (string.IsNullOrEmpty(jwt))
        {
            return null;
        }

        var payload = jwt.Split('.')[1];
        var token = Base64Url.Decode(payload);
        if (token == null) return null;
        var localClaimsIdentityModel = JsonConvert.DeserializeObject<Dictionary<string, object>>(token);

        return localClaimsIdentityModel == null ? null : CreateClaimsIdentity(localClaimsIdentityModel);
    }

    private ClaimsIdentity CreateClaimsIdentity(Dictionary<string, object> localClaimsIdentity)
    {
        var claimsIdentity = new ClaimsIdentity();
        foreach (var claim in localClaimsIdentity)
        {
            claimsIdentity.AddClaim(new Claim(claim.Key, claim.Value.ToString()!));
        }

        return claimsIdentity;
    }

    private async Task<ConsentRequest> CreateConsentRequest(Accreditation accreditation, List<EvidenceCode> evidenceCodesRequiringConsent, LocalizedString consentRequestStrings)
    {
        // At this point we can assume that both the subject and party are norwegian as this is enforced by RequirementValidatorService.ValidateConsent
        // In order to make the subjectName identical to Altinns version (particularly in tt02 cases), we get the subjectName from Altinn Serviceowner API
        //
        // TODO! We currently have no way of getting the last name of the person which is required for creating consent requests to non-organizations, so
        // this will for now fail.

        var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
        var subjectName = await GetPartyDisplayName(accreditation.SubjectParty, useAltinn: true);

        var consentResources = new List<ConsentRequestResource>();
        foreach (var ec in evidenceCodesRequiringConsent)
        {
            (string serviceCode, int serviceEdtionCode) = GetServiceCodeAndEditionFromEvidenceCode(ec);

            consentResources.Add(new ConsentRequestResource()
            {
                ServiceCode = serviceCode,
                ServiceEditionCode = serviceEdtionCode,
                Metadata = new Dictionary<string, string>()
                {
                    { "requestor", accreditation.Requestor! },
                    { "requestorName", requestorName }
                }
            });
        }

        var consentRequest = new ConsentRequest()
        {
            RequestMessage = new Dictionary<string, string>()
            {
                // We have already made sure they all belong to the same service context
                { "no-nb", consentRequestStrings.NoNb! },
                { "no-nn", consentRequestStrings.NoNn! },
                { "en", consentRequestStrings.En! }
            },
            RequestResources = consentResources,
            CoveredBy = accreditation.Requestor!,
            HandledBy = Settings.AltinnOrgNumber,
            OfferedBy = accreditation.Subject!,
            OfferedByName = subjectName,
            RedirectUrl = Settings.GetConsentRedirectUrl(accreditation.AccreditationId, accreditation.GetHmac()),
            ValidTo = accreditation.ValidTo
        };

        var request = new HttpRequestMessage(HttpMethod.Post, Settings.AltinnServiceAddress + "api/consentRequests?ForceEIAuthentication");
        request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnApiKey);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.JsonContent(consentRequest);

        try
        {
            request.SetAllowedErrorCodes(HttpStatusCode.BadRequest);
            var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errors = JsonConvert.DeserializeObject<List<ConsentRequestError>>(await response.Content.ReadAsStringAsync());
                var errorString = string.Empty;
                if (errors != null)
                {
                    foreach (var e in errors)
                    {
                        errorString += "ErrorCode:" + e.ErrorCode + " ErrorMessage:" + e.ErrorMessage + " ";
                    }
                }

                _logger.LogError("Failed to create consent request for AccreditationId={accreditationId}, Subject={subject}, StatusCode={statusCode}, ReasonPhrase={reasonPhrase}, ConsentErrors={consentErrors}, RequestJson={requestJson}",
                    accreditation.AccreditationId, accreditation.SubjectParty, response.StatusCode, response.ReasonPhrase, errorString, request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync());
                throw new ServiceNotAvailableException("Altinn denied the consent request. This is an internal error, please contact support");
            }

            var cr = JsonConvert.DeserializeObject<ConsentRequest>(await response.Content.ReadAsStringAsync());
            if (cr == null)
            {
                throw new Exception("Deserialize returned null");
            }

            return cr;
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to create consent request (no/invalid error model returned, no response) for AccreditationId={accreditationId}, Subject={subject}, Exception={ex}",
                accreditation.AccreditationId, accreditation.SubjectParty, ex.Message);

            throw;
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
        var req = ec.AuthorizationRequirements.OfType<ConsentRequirement>().First();

        return (req.ServiceCode, req.ServiceEdition);
    }

    private string GetConsentRequestUrl(ConsentRequest consentRequest)
    {
        // TODO! This should be replaced by handling the hal+json response from Altinn, and parsing the "guilink" href
        return Settings.AltinnPortalAddress + "ui/AccessConsent/request?id=" + consentRequest.AuthorizationCode;
    }

    private async Task<string> GetPartyDisplayName(Party party, bool useAltinn = false)
    {
        // TODO! We do not have any way of looking up the name of the subject, which is required for creating consent requests to persons
        if (party.NorwegianOrganizationNumber == null) return party.ToString();

        return useAltinn
            ? await GetOrganizationNameFromAltinn(party.NorwegianOrganizationNumber)
            : await GetOrganizationName(party.NorwegianOrganizationNumber);
    }

    private async Task<string> GetOrganizationName(string orgNr)
    {
        var entity = await _entityRegistryService.Get(orgNr);
        if (entity == null)
        {
            throw new InvalidSubjectException($"{orgNr} was not found in the Central Coordinating Register for Legal Entities");
        }

        return entity.Name;
    }

    private async Task<string> GetOrganizationNameFromAltinn(string subject)
    {
        // Because the organization API in Altinn before 20.11 returns "EditedName" instead of "UnitName" from ER, we may get mistmatches here
        // since the offeredBy check uses "UnitName". Therefore, we always use ER in PROD.
        if (!Settings.AltinnServiceOwnerApiUri.ToLower().Contains("tt02"))
        {
            return await GetOrganizationName(subject);
        }

        var organization = await _altinnServiceOwnerApiService.GetOrganization(subject);

        if (organization == null)
            throw new InvalidSubjectException($"{subject} was not found in Altinn and can not receive consent request");
        else
            return organization.Name!;
    }

    private async Task SendCorrespondence(Accreditation accreditation, IServiceContextTextTemplate<string> renderedTexts)
    {
        if (_correspondenceService == null)
        {
            throw new ServiceNotAvailableException("IAltinnService could not be resolved");
        }

        CorrespondenceDetails correspondence = CreateCorrespondence(accreditation, renderedTexts);
        try
        {
            await _correspondenceService.SendCorrespondence(correspondence);
        }
        catch (AltinnServiceException ex)
        {
            throw new ServiceNotAvailableException($"Failed to send correspondence to {accreditation.Subject}", ex);
        }
    }

    private CorrespondenceDetails CreateCorrespondence(Accreditation accreditation, IServiceContextTextTemplate<string> renderedTexts)
    {
        var correspondence = new CorrespondenceDetails();

        correspondence.Title = renderedTexts.CorrespondenceTitle;
        correspondence.Summary = renderedTexts.CorrespondenceSummary;
        correspondence.Body = renderedTexts.CorrespondenceBody;

        correspondence.Sender = renderedTexts.CorrespondenceSender;
        correspondence.Reportee = accreditation.Subject;
        correspondence.Notification = new NotificationDetails
        {
            SmsText = renderedTexts.SMSNotificationContent,
            EmailSubject = renderedTexts.EmailNotificationSubject,
            EmailBody = renderedTexts.EmailNotificationContent
        };

        return correspondence;
    }


    /// <summary>
    /// The model for posting use of a auth code
    /// </summary>
    public class LogUseModel
    {
        /// <summary>
        /// Service Code
        /// </summary>
        public string ServiceCode { get; set; } = string.Empty;

        /// <summary>
        /// Service Edition Code
        /// </summary>
        public int ServiceEditionCode { get; set; }

        /// <summary>
        /// Usage Date Time
        /// </summary>
        public string UsageDateTime { get; set; } = string.Empty;
    }
}