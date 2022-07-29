using System.Net;
using Dan.Common.Enums;
using Dan.Common.Helpers.Util;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
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
        private readonly ILogger<FuncEvidenceHarvester> _logger;

        public FuncEvidenceHarvester(IConsentService consentService, 
                                     IRequestContextService requestContextService,
                                     IEvidenceHarvesterService evidenceHarvesterService,
                                     IAccreditationRepository accreditationRepository,
                                     ILoggerFactory loggerFactory)
        {
            _consentService = consentService;
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

            var accreditation = await _accreditationRepository.GetAccreditationAsync(accreditationId, _requestContextService);
            if (accreditation == null)
            {
                throw new NonExistentAccreditationException("The supplied accreditation id was not found or authorization for it failed");
            }
            
            var evidence = await _evidenceHarvesterService.Harvest(evidenceCodeName, accreditation, _evidenceHarvesterService.GetEvidenceHarvesterOptionsFromRequest());

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (req.HasQueryParam("envelope") && !req.GetBoolQueryParam("envelope"))
            {
                await response.SetUnenvelopedEvidenceValuesAsync(evidence.EvidenceValues);
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
                    _logger.LogInformation("Start logging consent based harvest aid={accreditaionId} evidenceCode={evidenceCode}", accreditation.AccreditationId, evidenceCode.EvidenceCodeName);
                    await LogConsentBasedHarvest(evidenceCode, accreditation);
                    _logger.LogInformation("Completed logging consent based harvest aid={accreditationId} evidenceCode={evidenceCode} elapsedMs={elapsedMs}", accreditation.AccreditationId, evidenceCode.EvidenceCodeName, t.ElapsedMilliseconds);
                }
            }
            // Save timestamp and evidencecode name for statistics
            (accreditation.DataRetrievals ??= new List<DataRetrieval>()).Add(new DataRetrieval() {EvidenceCodeName = evidenceCodeName, TimeStamp = DateTime.Now});

            await _accreditationRepository.UpdateAccreditationAsync(accreditation);

            _logger.DanLog(accreditation, LogAction.DataRetrieved);

            return response;
        }

        private Task<bool> LogConsentBasedHarvest(EvidenceCode evidence, Accreditation accreditation)
        {
            return _consentService.LogUse(accreditation, evidence, DateTime.Now);
        }

    }
}
