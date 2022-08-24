using System.Net;
using System.Net.Http.Headers;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Attributes;
using Dan.Core.Config;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    /// <summary>
    /// Function for showing a receipt to the user after granting or denying a receipt
    /// </summary>
    public class FuncConsentReceipt
    {

        private readonly IEntityRegistryService _entityRegistryService;
        private readonly IServiceContextService _serviceContextService;
        private readonly IAccreditationRepository _accreditationRepository;
        private readonly ILogger<FuncConsentReceipt> _logger;

        private const string AboutUrl = "https://www.altinndigital.no/produkter/data.altinn/";

        /// <summary>
        /// Creates an instance of <see cref="FuncConsentReceipt"/> 
        /// </summary>
        /// <param name="entityRegistryService"></param>
        /// <param name="serviceContextService"></param>
        /// <param name="accreditationRepository"></param>
        /// <param name="loggerFactory"></param>
        public FuncConsentReceipt(IEntityRegistryService entityRegistryService, IServiceContextService serviceContextService, IAccreditationRepository accreditationRepository, ILoggerFactory loggerFactory)
        {
            _entityRegistryService = entityRegistryService;
            _serviceContextService = serviceContextService;
            _accreditationRepository = accreditationRepository;
            _logger = loggerFactory.CreateLogger<FuncConsentReceipt>();
        }

        /// <summary>
        /// Redirect URL endpoint for consent process that should return HTML for viewing in a web browser and also save the received authorizationCode with the accreditation
        /// </summary>
        /// <param name="req">The HTTP request</param>
        /// <param name="accreditationId">The accreditation id</param>
        /// <returns>A response with HTML / CSS</returns>
        [Function("FuncConsentReceipt"), HtmlError, NoAuthentication]
        public async Task<HttpResponseData> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consent/{accreditationId}")] 
            HttpRequestData req,
            string accreditationId)
        {
            // Pass null as partitionKeyValue since we're unauthenticated. This will cause a cross partition query.
            var accreditation = await _accreditationRepository.GetAccreditationAsync(accreditationId, null);
            
            if (accreditation == null)
            {
                throw new NonExistentAccreditationException();
            }

            if (accreditation.ValidTo < DateTime.Now)
            {
                throw new ExpiredAccreditationException();
            }

            if (!req.HasQueryParam("hmac") || !req.HasQueryParam("status"))
            {
                var response = req.CreateHtmlResponse(HttpStatusCode.BadRequest, "Error.html", new { title = "Invalid request", message = "Missing hmac/status", ebevisInfo = $"For mer informasjon om l&oslash;sningen data.altinn.no, g&aring; til <a href={AboutUrl}</a>" });
                response.Headers.TryAddWithoutValidation("X-Consent-Success", "false");
                return response;
            }

            if (!ValidateHMac(accreditation, req.GetQueryParam("hmac")))
            {
                var response = req.CreateHtmlResponse(HttpStatusCode.BadRequest, "Error.html", new { title = "Invalid request", message = "Invalid hmac", ebevisInfo = $"For mer informasjon om l&oslash;sningen data.altinn.no, g&aring; til <a href={AboutUrl}</a>" });
                response.Headers.TryAddWithoutValidation("X-Consent-Success", "false");
                return response;
            }

            accreditation.LastChanged = DateTime.Now;

            var subjectName = await GetPartyDisplayName(accreditation.SubjectParty);
            var requestorName = await GetPartyDisplayName(accreditation.RequestorParty);

            var serviceContexts = await _serviceContextService.GetRegisteredServiceContexts();
            var currentServiceContext = serviceContexts.First(x => x.Name == accreditation.ServiceContext);
            var renderedTexts = TextTemplateProcessor.GetRenderedTexts(currentServiceContext, accreditation, requestorName, subjectName, "");


            var serviceContextName = !string.IsNullOrEmpty(accreditation.ServiceContext) 
                ? accreditation.ServiceContext 
                : accreditation.EvidenceCodes.First().GetBelongsToServiceContexts().FirstOrDefault();

            if (IsStatusAccepted(req.GetQueryParam("status")))
            {
                accreditation.AuthorizationCode = req.GetQueryParam("authorizationcode");
                if (accreditation.AuthorizationCode == null)
                {
                    var response = req.CreateHtmlResponse(HttpStatusCode.BadRequest, "Error.html", new { title = "Invalid request", message = "Missing authorization code", ebevisInfo = $"For mer informasjon om l&oslash;sningen data.altinn.no, g&aring; til <a href={AboutUrl}</a>" });
                    response.Headers.TryAddWithoutValidation("X-Consent-Success", "false");
                    return response;
                }

                await _accreditationRepository.UpdateAccreditationAsync(accreditation);
                _logger.DanLog(accreditation, LogAction.ConsentGiven);

                if (string.IsNullOrEmpty(accreditation.ConsentReceiptRedirectUrl))
                {
                    var response = req.CreateHtmlResponse(HttpStatusCode.OK, "ConsentGiven.html", new
                    {
                        title = renderedTexts.ConsentTitleText,
                        message = renderedTexts.ConsentGivenReceiptText,
                        ebevisInfo = $"For mer informasjon om l&oslash;sningen data.altinn.no, g&aring; til <a href=\"{AboutUrl}\"</a>",
                        altinnurl = Settings.AltinnPortalAddress + "ui/messagebox",
                        altinnmessage = "Tilbake til meldingsboksen",
                        servicecontext = serviceContextName
                    });
                    response.Headers.TryAddWithoutValidation("X-Consent-Success", "true");
                    return response;
                }

                return CreateRedirectResponse(req, accreditation, req.GetQueryParam("status")!);
            }

            accreditation.AuthorizationCode = ConsentService.ConsentDenied;
            await _accreditationRepository.UpdateAccreditationAsync(accreditation);

            _logger.DanLog(accreditation, LogAction.ConsentDenied);

            if (string.IsNullOrEmpty(accreditation.ConsentReceiptRedirectUrl))
            {
                var response = req.CreateHtmlResponse(HttpStatusCode.OK, "ConsentGiven.html", new
                {
                    title = renderedTexts.ConsentTitleText,
                    message = renderedTexts.ConsentDeniedReceiptText,
                    @ebevisInfo = $"For mer informasjon om l&oslash;sningen data.altinn.no, g&aring; til <a href=\"{AboutUrl}\"</a>",
                    @altinnurl = Settings.AltinnPortalAddress + "ui/messagebox",
                    @altinnmessage = "G&aring; tilbake til Altinn",
                    @servicecontext = serviceContextName
                });
                response.Headers.TryAddWithoutValidation("X-Consent-Success", "false");
                return response;
            }

            return CreateRedirectResponse(req, accreditation, req.GetQueryParam("status")!);
        }

        private HttpResponseData CreateRedirectResponse(HttpRequestData req, Accreditation accreditation, string status)
        {
            var response = req.CreateResponse(HttpStatusCode.Moved);
            response.Headers.Add("Location", accreditation.ConsentReceiptRedirectUrl + $"?status={status}&id={accreditation.AccreditationId}");
            response.Headers.Add("Cache-Control", (new CacheControlHeaderValue()
            {
                NoCache = true,
                MustRevalidate = true
            }).ToString());

            return response;
        }

        private async Task<string> GetPartyDisplayName(Party party)
        {
            // TODO! Look up name of person? Party.ToString() will handle redacting.
            if (party.NorwegianOrganizationNumber == null) return party.ToString();

            var result = await _entityRegistryService.GetOrganizationEntry(party.NorwegianOrganizationNumber);
            return result?.Navn ?? party.NorwegianOrganizationNumber;
        }

        private static bool ValidateHMac(Accreditation accreditation, string? hmac)
        {
            foreach (var secret in Settings.ConsentValidationSecrets)
            {
                var hash = accreditation.GetHmac(secret);
                if (hash == hmac)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsStatusAccepted(string? statusResponse)
        {
            return statusResponse != null && statusResponse.ToLowerInvariant() == "ok";
        }
    }
}
