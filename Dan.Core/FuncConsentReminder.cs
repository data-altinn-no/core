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
    /// The Azure function rendering the list of accreditations for the given client certificate
    /// </summary>
    public class FuncConsentReminder
    {
        private readonly IAltinnCorrespondenceService _altinnCorrespondenceService;
        private readonly IConsentService _consentService;
        private readonly IRequestContextService _requestContextService;
        private readonly IAccreditationRepository _applicationRepository;
        private readonly ILogger<FuncConsentReminder> _logger;

        public FuncConsentReminder(
            IAltinnCorrespondenceService altinnCorrespondenceService,
            IConsentService consentService,
            IRequestContextService requestContextService,
            IAccreditationRepository applicationRepository,
            ILoggerFactory loggerFactory)
        {            
            _altinnCorrespondenceService = altinnCorrespondenceService;
            _consentService = consentService;
            _requestContextService = requestContextService;
            _applicationRepository = applicationRepository;
            _logger = loggerFactory.CreateLogger<FuncConsentReminder>();
        }

        [Function("FuncConsentReminderGet")]
        public async Task<HttpResponseData> Get(
           [HttpTrigger(AuthorizationLevel.Function, "get", Route = "accreditations/{accreditationId}/reminders")] HttpRequestData req,
           string accreditationId)
        {
            await _requestContextService.BuildRequestContext(req);

            // TODO! Should we allow GET on expired accreditations? (FuncEvidenceStatus does not)
            var accreditation = await _applicationRepository.GetAccreditationAsync(accreditationId, _requestContextService.AuthenticatedOrgNumber);

            return accreditation != null 
                ? req.CreateExternalResponse(HttpStatusCode.OK, accreditation.Reminders)
                : req.CreateResponse(HttpStatusCode.Forbidden);
        }

        [Function("FuncConsentReminderPost")]
        public async Task<HttpResponseData> Post(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "accreditations/{accreditationId}/reminders")] HttpRequestData req,
            string accreditationId)
        {
            await _requestContextService.BuildRequestContext(req);

            var accreditation = await _applicationRepository.GetAccreditationAsync(accreditationId, _requestContextService.AuthenticatedOrgNumber);
            if (accreditation == null)
            {
                return req.CreateResponse(HttpStatusCode.Forbidden);
            }

            ValidateAccreditationForReminder(accreditationId, accreditation);

            var response = await _altinnCorrespondenceService.SendNotification(accreditation, _requestContextService.ServiceContext);
            accreditation.Reminders.AddRange(response);

            await _applicationRepository.UpdateAccreditationAsync(accreditation);
            _logger.DanLog(accreditation, LogAction.ConsentReminderSent);         
            
            return req.CreateExternalResponse(HttpStatusCode.OK, response);
        }

        private void ValidateAccreditationForReminder(string accreditationId, Accreditation accr)
        {
            var evidenceCodesRequiringConsent = _consentService.GetEvidenceCodesRequiringConsentForActiveContext(accr);
            if (evidenceCodesRequiringConsent.Count < 1)
                throw new RequiresConsentException($"There are no evidence codes requiring subject action");

            if (!string.IsNullOrEmpty(accr.AuthorizationCode))
                throw new ConsentAlreadyHandledException($"Consent has already been given or rejected for {accreditationId}");

            if (accr.ValidTo < DateTime.Now)
                throw new ExpiredConsentException("The consent for this accreditation is expired");

            if (accr.Reminders.OrderByDescending(x=>x.Date).First().Date>DateTime.Now.AddDays(-7))
                throw new AuthorizationFailedException("Reminders have already been sent the the last week");

            if (accr.Subject == null)
                throw new InvalidSubjectException(
                    $"Cannot send reminder to foreign party identified by '{accr.SubjectParty}'");
        }
    }
}
