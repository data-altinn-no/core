using System.Net;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Exceptions;
using Dan.Core.Extensions;
using Dan.Core.Services;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core
{
    /// <summary>
    /// The Azure function handling direct harvesting of data for evidence codes meeting certain conditions:
    /// Open, synchronous and no consent requirement
    /// </summary>
    public class FuncDirectHarvester
    {
        private readonly IAuthorizationRequestValidatorService _authorizationRequestValidatorService;
        private readonly IEvidenceHarvesterService _evidenceHarvesterService;
        private readonly IRequestContextService _requestContextService;
        private readonly ILogger<FuncDirectHarvester> _logger;

        public FuncDirectHarvester(
            IAuthorizationRequestValidatorService authorizationRequestValidatorService,
            IRequestContextService requestContextService, 
            IEvidenceHarvesterService evidenceHarvesterService,
            ILoggerFactory loggerFactory)
        {
            _authorizationRequestValidatorService = authorizationRequestValidatorService;
            _requestContextService = requestContextService;
            _evidenceHarvesterService = evidenceHarvesterService;
            _logger = loggerFactory.CreateLogger<FuncDirectHarvester>();
        }
        /// <summary>
        /// The function entry point
        /// </summary>
        /// <param name="req"></param>
        /// <param name="evidenceCodeName">
        /// The evidence code requested.
        /// </param>
        /// <returns>
        ///  The <see cref="HttpResponseData"/>.
        /// </returns>
        [Function("DirectHarvester")]
        public async Task<HttpResponseData> RunAsync(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "directharvest/{evidenceCodeName}")]
            HttpRequestData req,
            string evidenceCodeName)
        {

            await _requestContextService.BuildRequestContext(req);

            var authorizationRequest = await GetAuthorizationRequest(req, evidenceCodeName);

            await _authorizationRequestValidatorService.Validate(authorizationRequest);
            var evidenceCodes = _authorizationRequestValidatorService.GetEvidenceCodes();
            
            if (evidenceCodes.Any(x => x.IsAsynchronous ||  (x.AuthorizationRequirements ?? new List<Requirement>()).Any(y => y is ConsentRequirement)))
            {
                throw new InvalidEvidenceRequestException("Unable to directly harvest async or consent-based evidence code");
            }

            foreach (var es in evidenceCodes)
            {
                es.AuthorizationRequirements = new List<Requirement>();
            }

            var validTo = _authorizationRequestValidatorService.GetValidTo();
            var accreditation = new Accreditation
            {
                AccreditationId = Guid.NewGuid().ToString(),
                EvidenceCodes = evidenceCodes,
                Requestor = authorizationRequest.Requestor,
                RequestorParty = authorizationRequest.RequestorParty,
                Subject = authorizationRequest.Subject,
                SubjectParty = authorizationRequest.SubjectParty,
                IsDirectHarvest = true,
                ConsentReference = null,
                ExternalReference = null,
                Issued = DateTime.Now,
                LastChanged = DateTime.Now,
                ValidTo = validTo,
                Owner = _requestContextService.AuthenticatedOrgNumber,
                ServiceContext = _requestContextService.ServiceContext.Name
            };

            var evidence = await _evidenceHarvesterService.Harvest(evidenceCodeName, accreditation, _requestContextService.GetEvidenceHarvesterOptionsFromRequest());
            _logger.DanLog(accreditation, LogAction.AuthorizationGranted);

            var response = req.CreateResponse(HttpStatusCode.OK);
            if (req.HasQueryParam("envelope") && !req.GetBoolQueryParam("envelope"))
            {
                await response.SetUnenvelopedEvidenceValuesAsync(evidence.EvidenceValues);
            }
            else
            {
                await response.SetEvidenceAsync(evidence);
            }

            _logger.DanLog(accreditation, LogAction.DataRetrieved);

            return response;
            
        }

        private static async Task<AuthorizationRequest> GetAuthorizationRequest(HttpRequestData req, string evidenceCodeName)
        {
            var requestor = req.GetQueryParam("requestor") ?? req.GetAuthenticatedPartyOrgNumber();
            var subject = req.GetQueryParam("subject") ?? await req.GetSubjectIdentifierFromPost();

            List<EvidenceParameter>? listOfEvidenceParameters = null;
            foreach (var key in req.GetQueryParams().AllKeys.Except(GetQueryParamsToSkip(), StringComparer.OrdinalIgnoreCase))
            {
                (listOfEvidenceParameters ??= new List<EvidenceParameter>())
                    .Add(new EvidenceParameter() { EvidenceParamName = key, Value = req.GetQueryParam(key!) });
            }

            var listOfEvidenceRequests = new List<EvidenceRequest>();
            var evidenceRequest = new EvidenceRequest()
            {
                EvidenceCodeName = evidenceCodeName,
                Parameters = listOfEvidenceParameters
            };
            listOfEvidenceRequests.Add(evidenceRequest);

            return new AuthorizationRequest()
            {
                Requestor = requestor,
                Subject = subject,
                EvidenceRequests = listOfEvidenceRequests
            };
        }

        private static IEnumerable<string> GetQueryParamsToSkip()
        {
            return new[]
            {
                "requestor",
                "subject",
                "code",
                "envelope",
                RequestContextService.QueryParamReuseToken,
                RequestContextService.QueryParamTokenOnBehalfOfOwner
            };
        }
    }
}
