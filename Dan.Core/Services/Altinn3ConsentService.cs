using Altinn.Dd.Correspondence.Models;
using Altinn.Dd.Correspondence.Services;
using Azure.Core;
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

        /// <summary>
        /// Magic string used as authorization code when the user has actively denied a consent request
        /// </summary>
        public const string ConsentDenied = "denied";

        /// <summary>
        /// Magic string for claim name for validTo
        /// </summary>
        public const string ConsentJwtValidtoKey = "exp";

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

        /// <summary>
        /// Check consent status for the given accreditation. If onlyLocalCheck is true, only check the local status of the consent request (i.e. whether it has been marked as denied or expired locally), and do not call Altinn to check the status of the consent request there. This is useful for scenarios where we want to avoid calling Altinn (e.g. when processing a batch of accreditations) and can accept a potentially stale consent status. If onlyLocalCheck is false, the method will call Altinn to check the status of the consent request there, which will give the most up-to-date status but will also be slower and put more load on Altinn.
        /// </summary>
        public async Task<ConsentStatus> Check(Accreditation accreditation, bool onlyLocalCheck)
        {
            var evidenceCodesRequiringConsent = GetEvidenceCodesRequiringConsentForActiveContext(accreditation);
            if (evidenceCodesRequiringConsent.Count == 0)
            {
                _logger.LogError("Expected at least one evidencecode in the accreditation requiring consent in accredition {accreditationId}", accreditation.AccreditationId);
            }

            if (accreditation.Altinn3ConsentStatus == null)
            {
                return ConsentStatus.Pending;
            }

            if (accreditation.Altinn3ConsentStatus == ConsentDenied)
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
            var evidenceCodeWithConsent = accreditation.EvidenceCodes
               .FirstOrDefault(x => x.AuthorizationRequirements.OfType<ConsentRequirement>().Any());

            if (evidenceCodeWithConsent == null)
            {
                return null;
            }
            var jwt = await GetJwt(accreditation, evidenceCodeWithConsent);

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

        /// <summary>
        /// Returns whether or not an evidence code has a consent requirement that applies to the current service context. If an evidence code has multiple consent requirements for different service contexts, it is sufficient that one of them applies to the current service context for the evidence code to require consent. If an evidence code has no consent requirements, it does not require consent.
        /// </summary>
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
        /// Returns all evidence codes on an accreditation that require consent for the active service context. If an evidence code has multiple consent requirements for different service contexts, it is sufficient that one of them applies to the current service context for the evidence code to be included in the result. If an evidence code has no consent requirements, it is not included in the result.
        /// </summary>
        public List<EvidenceCode> GetEvidenceCodesRequiringConsentForActiveContext(Accreditation accreditation)
        {
            return accreditation.EvidenceCodes.Where(EvidenceCodeRequiresConsent).ToList();
        }

        /// <summary>
        /// Attempts to retrieve a consent token (Jwt) from maskinporten for the current accreditation and evidence code.
        /// </summary>
        public async Task<string> GetJwt(Accreditation accreditation, EvidenceCode evidenceCode)
        {
            if (string.IsNullOrEmpty(accreditation.Altinn3ConsentId) && string.IsNullOrEmpty(accreditation.AuthorizationCode))
            {
                throw new RequiresConsentException("The accreditation is missing a valid Altinn consent id.");
            }
    
            try
            {
                // The consentId can be either the Altinn3ConsentId or the old AuthorizationCode which is migrated over to Altinn 3 for retrieval
                var consentId = !string.IsNullOrEmpty(accreditation.Altinn3ConsentId) ? accreditation.Altinn3ConsentId : accreditation.AuthorizationCode;

                var token = await _tokenRequesterService.GetMaskinportenConsentToken(consentId, accreditation.SubjectParty.GetAsString(false), evidenceCode);
                
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

            accreditation.AltinnConsentUrl = consentresponse.ViewUri;
            accreditation.Altinn3ConsentId = consentresponse.Id;

            var renderedTexts = TextTemplateProcessor.GetRenderedTexts(_requestContextService.ServiceContext, accreditation, requestorName, subjectName, accreditation.AltinnConsentUrl, true);

            if (!skipAltinnNotification)
            {
                await SendCorrespondence(accreditation, renderedTexts);
                _logger.DanLog(accreditation, LogAction.CorrespondenceSent);
            }
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
                var result = await _correspondenceService.SendCorrespondence(correspondence);                

                if (!result.IsSuccess || result.IsFailure)
                {
                    _logger.LogError("Failed to send consent correspondence for AccreditationId={accreditationId}, Subject={subject}, IsFailure={isFailure}, Error={error}",
                       accreditation.AccreditationId, accreditation.SubjectParty, result.IsFailure, result.Error);

                    throw new ServiceNotAvailableException($"Failed to send correspondence to {accreditation.SubjectParty.GetAsString()}: {result.Error}");

                }
                else
                {   _logger.LogInformation("Successfully sent consent correspondence for AccreditationId={accreditationId}, Subject={subject}, CorrespondenceId={correspondenceId}",
                        accreditation.AccreditationId, accreditation.SubjectParty.GetAsString(), result.Receipt?.IdempotencyKey);
                }

            }
            catch (ServiceNotAvailableException ex)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new ServiceNotAvailableException($"Failed to send correspondence to {accreditation.SubjectParty.GetAsString()}", ex);
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
                Body = renderedTexts.CorrespondenceBodyA3,
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

        private async Task<Altinn3ConsentResponse> CreateConsentRequest(Accreditation accreditation, List<EvidenceCode> evidenceCodesRequiringConsent, LocalizedString consentRequestStrings)
        {
            // At this point we can assume that both the subject and party are norwegian as this is enforced by RequirementValidatorService.ValidateConsent
            // In order to make the subjectName identical to Altinns version (particularly in tt02 cases), we get the subjectName from Altinn Serviceowner API
            //
            // TODO! We currently have no way of getting the last name of the person which is required for creating consent requests to non-organizations, so
            // this will for now fail.

            //var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);
            //var subjectName = await GetPartyDisplayName(accreditation.SubjectParty, useAltinn: true);;


            var consentRequest = new Altinn3ConsentRequest()
            {
                ConsentRights = new List<ConsentRight>(),
                Id = Guid.NewGuid().ToString(),
                From = accreditation.SubjectParty.GetAltinnFormat(),
                To = accreditation.RequestorParty.GetAltinnFormat(),
                RedirectUrl = Settings.GetConsentRedirectUrl(accreditation.AccreditationId, accreditation.GetHmac()),
                ValidTo = accreditation.ValidTo,
                PortalViewMode = "show"
            };

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
                    Resource = new List<ConsentResource>()
                    {
                        new ConsentResource()
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
                            errorString += "ErrorCode:" + e.Title + " ErrorMessage:" + e.Detail + " ";
                        }
                    }

                    _logger.LogError("Failed to create consent request for AccreditationId={accreditationId}, Subject={subject}, StatusCode={statusCode}, ReasonPhrase={reasonPhrase}, ConsentErrors={consentErrors}, RequestJson={requestJson}",
                        accreditation.AccreditationId, accreditation.SubjectParty, response.StatusCode, response.ReasonPhrase, errorString, request.Content == null ? string.Empty : await request.Content.ReadAsStringAsync());
                    throw new ServiceNotAvailableException("Altinn denied the consent request. This is an internal error, please contact support");
                }

                var cr = JsonConvert.DeserializeObject<Altinn3ConsentResponse>(await response.Content.ReadAsStringAsync());
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
