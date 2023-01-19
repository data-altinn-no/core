using System.Net;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    /// <summary>
    /// The Azure Function being the proxy of calls to the various evidence sources
    /// </summary>
    public class FuncEvidenceHarvester
    {
        private readonly IConsentService _consentService;
        private readonly IEvidenceHarvesterService _evidenceHarvesterService;
        private readonly IAccreditationRepository _accreditationRepository;
        private readonly IRequestContextService _requestContextService;
        private readonly IAuthorizationRequestValidatorService _authorizationRequestValidatorService;
        private readonly ILogger<FuncEvidenceHarvester> _logger;

        public FuncEvidenceHarvester(IConsentService consentService,
                                     IRequestContextService requestContextService,
                                     IAuthorizationRequestValidatorService authorizationRequestValidatorService,
                                     IEvidenceHarvesterService evidenceHarvesterService,
                                     IAccreditationRepository accreditationRepository,
                                     ILoggerFactory loggerFactory)
        {
            _consentService = consentService;
            _authorizationRequestValidatorService = authorizationRequestValidatorService;
            _evidenceHarvesterService = evidenceHarvesterService;
            _accreditationRepository = accreditationRepository;
            _requestContextService = requestContextService;
            _logger = loggerFactory.CreateLogger<FuncEvidenceHarvester>();
        }

        /// <summary>
        /// The function entry point
        /// </summary>
        /// <param name="req">
        /// The HTTP request. This is internally available only.
        /// </param>
        /// <param name="accreditationId">
        /// The accreditation id this relates to.
        /// </param>
        /// <param name="evidenceCodeName">
        /// The evidence code requested.
        /// </param>
        /// <returns>
        /// The <see cref="HttpResponseMessage"/>.
        /// </returns>
        [Function("EvidenceHarvester")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "evidence/{accreditationId}/{evidenceCodeName}")]
            HttpRequestData req,
            string accreditationId,
            string evidenceCodeName)
        {
            await _requestContextService.BuildRequestContext(req);

            var accreditation = await _accreditationRepository.GetAccreditationAsync(accreditationId,
                _requestContextService.AuthenticatedOrgNumber);
            if (accreditation == null)
            {
                throw new NonExistentAccreditationException(
                    "The supplied accreditation id was not found or authorization for it failed");
            }

            var authorizationRequest = GetAuthorizationRequest(accreditation);
            await _authorizationRequestValidatorService.Validate(authorizationRequest);

            var evidence = await _evidenceHarvesterService.Harvest(evidenceCodeName, accreditation, 
                _requestContextService.GetEvidenceHarvesterOptionsFromRequest());

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (req.HasQueryParam("envelope") && !req.GetBoolQueryParam("envelope"))
            {
                await response.SetUnenvelopedEvidenceValuesAsync(evidence.EvidenceValues, req.GetQueryParam(JmesPathTransfomer.QueryParameter));
            }
            else
            {
                await response.SetEvidenceAsync(evidence);
            }

            var evidenceCode = accreditation.GetValidEvidenceCode(evidenceCodeName);

            if (_consentService.EvidenceCodeRequiresConsent(evidenceCode))
            {
                using (var t = _logger.Timer("consent-log-usage"))
                {
                    _logger.LogInformation(
                        "Start logging consent based harvest aid={accreditationId} evidenceCode={evidenceCode}",
                        accreditation.AccreditationId, evidenceCode.EvidenceCodeName);
                    await LogConsentBasedHarvest(evidenceCode, accreditation);
                    _logger.LogInformation(
                        "Completed logging consent based harvest aid={accreditationId} evidenceCode={evidenceCode} elapsedMs={elapsedMs}",
                        accreditation.AccreditationId, evidenceCode.EvidenceCodeName, t.ElapsedMilliseconds);
                }
            }

            // Save timestamp and evidencecode name for statistics
            accreditation.DataRetrievals.Add(new DataRetrieval()
                { EvidenceCodeName = evidenceCodeName, TimeStamp = DateTime.Now });

            await _accreditationRepository.UpdateAccreditationAsync(accreditation);

            _logger.DanLog(accreditation, LogAction.DataRetrieved);

            return response;
        }

        private Task<bool> LogConsentBasedHarvest(EvidenceCode evidence, Accreditation accreditation)
        {
            return _consentService.LogUse(accreditation, evidence, DateTime.Now);
        }

        private static AuthorizationRequest GetAuthorizationRequest(Accreditation accreditation)
        {
            var listOfEvidenceRequests = new List<EvidenceRequest>();
            foreach (var ec in accreditation.EvidenceCodes)
            {
                listOfEvidenceRequests.Add(new EvidenceRequest()
                {
                    EvidenceCodeName = ec.EvidenceCodeName,
                    Parameters = ec.Parameters
                });
            }

            return new AuthorizationRequest()
            {
                Requestor = accreditation.Requestor,
                Subject = accreditation.Subject,
                EvidenceRequests = listOfEvidenceRequests,
                FromEvidenceHarvester = true
            };
        }
    }
}
