using Altinn.Dd.Correspondence.Models;
using Altinn.Dd.Correspondence.Services;
using Dan.Common.Enums;
using Dan.Common.Interfaces;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Security.Claims;


namespace Dan.Core.Services
{
    public class Altinn3ConsentService : IAltinn3ConsentService
    {

        private readonly HttpClient _httpClient;
        private readonly HttpClient _noCertHttpClient;
        private readonly ILogger<ConsentService> _logger;
        private readonly IDdCorrespondenceService _correspondenceService;
        private readonly Interfaces.IEntityRegistryService _entityRegistryService;
        private readonly IAltinnServiceOwnerApiService _altinnServiceOwnerApiService;
        private readonly IRequestContextService _requestContextService;
        private readonly ITokenRequesterService _tokenRequesterService;

        private const string AltinnResourcePrefixOrg = "urn:altinn:organization:identifier-no";
        private const string AltinnResourcePrefixPerson = "urn:altinn:person:identifier-no";

        /// <summary>
        /// Magic string used as authorization code when the user has actively denied a consent request
        /// </summary>
        public const string ConsentDenied = "denied";

        /// <summary>
        /// Magic string for claim name for validTo
        /// </summary>
        public const string ConsentJwtValidtoKey = "ValidToDate";

        public Altinn3ConsentService(
            HttpClient httpClient,
            HttpClient noCertHttpClient,
            ILogger<ConsentService> logger,
            IDdCorrespondenceService correspondenceService,
            Interfaces.IEntityRegistryService entityRegistryService,
            IAltinnServiceOwnerApiService altinnServiceOwnerApiService,
            IRequestContextService requestContextService,
            ITokenRequesterService tokenRequesterService)
        {
            _httpClient = httpClient;
            _noCertHttpClient = noCertHttpClient;
            _logger = logger;
            _correspondenceService = correspondenceService;
            _entityRegistryService = entityRegistryService;
            _altinnServiceOwnerApiService = altinnServiceOwnerApiService;
            _requestContextService = requestContextService;
            _tokenRequesterService = tokenRequesterService;
        }

        public async Task<ConsentStatus> Check(Accreditation accreditation, bool onlyLocalCheck)
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

        private async Task<ClaimsIdentity?> GetClaims(Accreditation accreditation)
        {
            var jwt = await GetJwt(accreditation);           

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


        public bool EvidenceCodeRequiresConsent(EvidenceCode evidenceCode)
        {
            if (!evidenceCode.AuthorizationRequirements.OfType<ConsentRequirement>().Any())
            {
                return false;
            }

            return evidenceCode.AuthorizationRequirements.OfType<ConsentRequirement>().Any(x =>
                x.AppliesToServiceContext.Count == 0 || x.AppliesToServiceContext.Contains(_requestContextService.ServiceContext.Name));
        }

        public List<EvidenceCode> GetEvidenceCodesRequiringConsentForActiveContext(Accreditation accreditation)
        {
            return accreditation.EvidenceCodes.Where(EvidenceCodeRequiresConsent).ToList();
        }


        public async Task<string> GetJwt(Accreditation accreditation)
        {
            if (string.IsNullOrEmpty(accreditation.Altinn3ConsentId))
            {
                throw new RequiresConsentException("The accreditation is missing a valid altinn 3 consent id.");
            }
    
            try
            {
                var token = await _tokenRequesterService.GetMaskinportenConsentToken(accreditation.Altinn3ConsentId, accreditation.SubjectParty.GetAsString(false));
                
                if (token == null)
                {
                    throw new ServiceNotAvailableException("Unable to parse consent token from Maskinporten");
                }

                return token;
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

           // await _altinnServiceOwnerApiService.EnsureSrrRights(accreditation.Requestor, accreditation.ValidTo.AddYears(10), evidenceCodesRequiringConsent);

            if (_requestContextService.ServiceContext.ServiceContextTextTemplate == null)
            {
                throw new ServiceContextException(
                    "ServiceContextTemplate was not defined, required for consent initiation");
            }

            var processedConsentRequestStrings = TextTemplateProcessor.ProcessConsentRequestMacros(_requestContextService.ServiceContext.ServiceContextTextTemplate.ConsentDelegationContexts, accreditation, requestorName, subjectName, _requestContextService.ServiceContext.Name);
            var consentresponse = await CreateConsentRequest(accreditation, evidenceCodesRequiringConsent, processedConsentRequestStrings);

            _logger.DanLog(accreditation, LogAction.ConsentRequested);
            foreach (var ec in evidenceCodesRequiringConsent)
            {
                _logger.DanLog(accreditation, LogAction.DatasetRequiringConsentRequested, ec.EvidenceCodeName);
            }

            accreditation.AltinnConsentUrl = consentresponse.viewUri;
            accreditation.Altinn3ConsentId = consentresponse.id;

            var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_requestContextService.ServiceContext, accreditation, requestorName, subjectName, accreditation.AltinnConsentUrl, true);

            if (!skipAltinnNotification)
            {
                _logger.DanLog(accreditation, LogAction.CorrespondenceSent);
                await SendCorrespondence(accreditation, renderedTexts);
            }
        }

        private string? GetConsentRequestUrl(Altinn3ConsentRequestResponse consentRequest)
        {
            throw new NotImplementedException();
        }

        public Task<bool> LogUse(Accreditation accreditation, EvidenceCode evidence, DateTime? dateTime)
        {
            //Not yet implemented in altinn3
            return Task.FromResult(true);
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
                throw new ServiceNotAvailableException("Could not resolve Altinn 3 Consent Service");
            }

            DdCorrespondenceDetails correspondence = CreateCorrespondence(accreditation, renderedTexts);
            try
            {
                await _correspondenceService.SendCorrespondence(correspondence);
            }
            catch (AltinnServiceException ex)
            {
                throw new ServiceNotAvailableException($"Failed to send correspondence to {accreditation.Subject}", ex);
            }
        }

        private DdCorrespondenceDetails CreateCorrespondence(Accreditation accreditation, IServiceContextTextTemplate<string> renderedTexts)
        {
            var result = new DdCorrespondenceDetails()
            {
                Recipient = accreditation.Subject,
                Sender = renderedTexts.CorrespondenceSender,
                Title = renderedTexts.CorrespondenceTitle,
                Summary = renderedTexts.CorrespondenceSummary,
                Body = renderedTexts.CorrespondenceBody,
                AllowForwarding = false,
                IdempotencyKey = Guid.NewGuid(),
                IgnoreReservation = false,
                ShipmentDatetime = DateTime.UtcNow,
                VisibleDateTime = DateTime.UtcNow,
                SendersReference = accreditation.AccreditationId,
                Notification = new Altinn.Dd.Correspondence.Models.NotificationDetails()
                {
                    EmailBody = renderedTexts.EmailNotificationContent,
                    EmailSubject = renderedTexts.EmailNotificationSubject,
                    EmailContentType = EmailContentType.Html,
                    SmsText = renderedTexts.SMSNotificationContent
                }
            };

            return result;
        }

        private async Task<Altinn3ConsentRequestResponse> CreateConsentRequest(Accreditation accreditation, List<EvidenceCode> evidenceCodesRequiringConsent, LocalizedString consentRequestStrings)
        {
            // At this point we can assume that both the subject and party are norwegian as this is enforced by RequirementValidatorService.ValidateConsent
            // In order to make the subjectName identical to Altinns version (particularly in tt02 cases), we get the subjectName from Altinn Serviceowner API
            //
            // TODO! We currently have no way of getting the last name of the person which is required for creating consent requests to non-organizations, so
            // this will for now fail.

            //var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
            //var subjectName = await GetPartyDisplayName(accreditation.SubjectParty, useAltinn: true);;
            
            var fromPrefix = accreditation.SubjectParty.GetAsString(false).Length == 11 ? AltinnResourcePrefixPerson : AltinnResourcePrefixOrg;
            var toPrefix = accreditation.RequestorParty.GetAsString(false).Length == 11 ? AltinnResourcePrefixPerson : AltinnResourcePrefixOrg;

            var consentRequest = new Altinn3ConsentRequest();

            consentRequest.ConsentRights = new List<ConsentRight>();
            consentRequest.Id = Guid.NewGuid().ToString();
            consentRequest.From = $"{fromPrefix}:{accreditation.SubjectParty.GetAsString(false)}";
            consentRequest.To = $"{toPrefix}:{accreditation.RequestorParty.GetAsString(false)}";
            consentRequest.RedirectUrl = Settings.GetConsentRedirectUrl(accreditation.AccreditationId, accreditation.GetHmac());
            consentRequest.ValidTo = accreditation.ValidTo;
            consentRequest.PortalViewMode = "show";

            foreach (var ec in evidenceCodesRequiringConsent)
            {
                //there should only be one consent requirement per evidence code per servicecontext
                var resourceIdentifier = ec.AuthorizationRequirements.Where(z => z is ConsentRequirement).Select(z => (ConsentRequirement)z).Single().AltinnResource;

                if (string.IsNullOrEmpty(resourceIdentifier))
                {
                    throw new Exception($"AltinnResource was not defined on the ConsentRequirement for evidence code {ec.EvidenceCodeName}, which is required for creating the consent request");
                }

                consentRequest.ConsentRights.Add(new ConsentRight()
                {
                    Action = new List<string>() { "consent" },
                    Resource = new List<Resource>()
                    {
                        new Resource()
                        {
                            Type = "urn:altinn:resource",
                            Value = resourceIdentifier
                        }
                    }
                });
            }                       

            var request = new HttpRequestMessage(HttpMethod.Post, Settings.Altinn3ConsentUrl);
            var token = JsonConvert.DeserializeObject<Dictionary<string, string>>(await _tokenRequesterService.GetMaskinportenToken("altinn:consentrequests.org"));
            //request.Headers.TryAddWithoutValidation("ApiKey", Settings.AltinnApiKey);
            request.Headers.TryAddWithoutValidation("Accept", "application/json");
            request.Headers.TryAddWithoutValidation("Authorization", $"Bearer {token["access_token"]}");
            request.JsonContent(consentRequest);

            _logger.LogInformation(JsonConvert.SerializeObject(consentRequest));

            try
            {
                request.SetAllowedErrorCodes(HttpStatusCode.BadRequest);
                var response = await _httpClient.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var errors = JsonConvert.DeserializeObject<List<Altinn3ConsentRequestError>>(responseContent);
                    var errorString = string.Empty;
                    if (errors != null)
                    {
                        foreach (var e in errors)
                        {
                            errorString += "ErrorCode:" + e.title + " ErrorMessage:" + e.detail + " ";
                        }
                    }

                    _logger.LogError("Failed to create consent request for AccreditationId={accreditationId}, Subject={subject}, StatusCode={statusCode}, ReasonPhrase={reasonPhrase}, ConsentErrors={consentErrors}, RequestJson={requestJson}",
                        accreditation.AccreditationId, accreditation.SubjectParty, response.StatusCode, response.ReasonPhrase, errorString, request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync());
                    throw new ServiceNotAvailableException("Altinn denied the consent request. This is an internal error, please contact support");
                }

                var cr = JsonConvert.DeserializeObject<Altinn3ConsentRequestResponse>(await response.Content.ReadAsStringAsync());
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

    }
}
