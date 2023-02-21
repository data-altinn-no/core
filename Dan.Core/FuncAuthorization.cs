using System.Net;
using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace Dan.Core;

/// <summary>
/// The Azure function handling initial authorization and creating a accreditation entry
/// </summary>
public class FuncAuthorization
{
    private readonly HttpClient _client;
    private readonly IConsentService _consentService;
    private readonly IAuthorizationRequestValidatorService _authorizationRequestValidatorService;
    private readonly IRequestContextService _requestContextService;
    private readonly IAccreditationRepository _accreditationRepository;
    private readonly ILogger<FuncAuthorization> _logger;

    public FuncAuthorization(
        ILoggerFactory loggerFactory,
        IHttpClientFactory httpClientFactory,
        IConsentService consentService,
        IAuthorizationRequestValidatorService authorizationRequestValidatorService,
        IRequestContextService requestContextService,
        IAccreditationRepository accreditationRepository)
    {
        _logger = loggerFactory.CreateLogger<FuncAuthorization>();
        _client = httpClientFactory.CreateClient("SafeHttpClient");
        _consentService = consentService;
        _authorizationRequestValidatorService = authorizationRequestValidatorService;
        _requestContextService = requestContextService;
        _accreditationRepository = accreditationRepository;
    }

    /// <summary>
    /// Entry point for the authorization endpoint receiving authorization requests. This is the 
    /// </summary>
    /// <param name="req">
    /// The HTTP request object.
    /// </param>
    /// <returns>
    /// A <see cref="HttpResponseData"/> with the accreditation.
    /// </returns>
    [Function("Authorization")]
    public async Task<HttpResponseData> RunAsync(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "authorization")]
        HttpRequestData req)
    {
        await _requestContextService.BuildRequestContext(req);
        var authRequest = await req.ReadFromJsonAsync<AuthorizationRequest>();

        await _authorizationRequestValidatorService.Validate(authRequest);

        authRequest = _authorizationRequestValidatorService.GetAuthorizationRequest()!;
        var evidenceCodes = _authorizationRequestValidatorService.GetEvidenceCodes();
        var validTo = _authorizationRequestValidatorService.GetValidTo();

        var accreditation = new Accreditation
        {
            AccreditationId = Guid.NewGuid().ToString(),
            EvidenceCodes = evidenceCodes,
            Requestor = authRequest.Requestor,
            RequestorParty = authRequest.RequestorParty,
            Subject = authRequest.Subject,
            SubjectParty = authRequest.SubjectParty,
            ConsentReference = authRequest.ConsentReference,
            ExternalReference = authRequest.ExternalReference,
            Issued = DateTime.Now,
            LastChanged = DateTime.Now,
            ValidTo = validTo,
            Owner = _requestContextService.AuthenticatedOrgNumber,
            LanguageCode = string.IsNullOrEmpty(authRequest.LanguageCode) ? Constants.LANGUAGE_CODE_NORWEGIAN_NB : authRequest.LanguageCode.ToLower(),
            ServiceContext = _requestContextService.ServiceContext.Name,
            ConsentReceiptRedirectUrl = authRequest.ConsentReceiptRedirectUrl
        };

        _logger.LogInformation("Creating accreditation aid={accreditationId} requestor={requestor} subject={subject} numcodes={numCodes} codes={codes}",
            accreditation.AccreditationId, accreditation.RequestorParty, accreditation.SubjectParty, evidenceCodes.Count,
            string.Join(",", evidenceCodes.Select(x => x.EvidenceCodeName)));

        foreach (var evidenceCode in evidenceCodes.FindAll(x => x.IsAsynchronous))
        {
            using (var t = _logger.Timer($"{evidenceCode.EvidenceCodeName}-init"))
            {
                _logger.LogInformation("Start init async evidenceCode={evidenceCode} aid={accreditationId}", evidenceCode.EvidenceCodeName, accreditation.AccreditationId);
                await EvidenceSourceHelper.InitAsynchronousEvidenceCodeRequest(accreditation, evidenceCode, _client);
                _logger.LogInformation("Completed init async evidenceCode={evidenceCode} aid={accreditationId} elapsedMs={elapsedMs}", evidenceCode.EvidenceCodeName, accreditation.AccreditationId, t.ElapsedMilliseconds);
            }
        }


        if (authRequest.EvidenceRequests.Any(x => x.RequestConsent == true))
        {
            using (var t = _logger.Timer("consent-init"))
            {
                _logger.LogInformation("Start init consent aid={accreditationId}", accreditation.AccreditationId);
                await _consentService.Initiate(accreditation, authRequest.SkipAltinnNotification);
                _logger.LogInformation("Completed init consent aid={accreditationId} elapsedMs={elapsedMs}", accreditation.AccreditationId, t.ElapsedMilliseconds);
            }
        }

        // Remove authreqs before saving accreditation
        foreach (var es in accreditation.EvidenceCodes)
        {
            es.AuthorizationRequirements = new List<Requirement>();
        }

        await _accreditationRepository.CreateAccreditationAsync(accreditation);

        var response = req.CreateExternalResponse(HttpStatusCode.OK, accreditation);
        response.Headers.TryAddWithoutValidation("Location", accreditation.GetUrl());

        _logger.LogInformation("Completed authorization request successfully for aid={accreditationId}", accreditation.AccreditationId);
        _logger.DanLog(accreditation, LogAction.AuthorizationGranted);
        foreach (var evidenceRequest in authRequest.EvidenceRequests)
        {
            _logger.DanLog(accreditation, LogAction.DatasetRequested, evidenceRequest.EvidenceCodeName);
        }

        return response;
    }
}