using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Helpers;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Services;

public class EvidenceStatusService : IEvidenceStatusService
{
    private readonly IAvailableEvidenceCodesService _availableEvidenceCodesService;
    private readonly IConsentService _consentService;
    private readonly IRequestContextService _requestContextService;
    private readonly ILogger<EvidenceStatusService> _logger;
    private readonly IHttpClientFactory _clientFactory;

    public EvidenceStatusService(
        IAvailableEvidenceCodesService availableEvidenceCodesService, 
        IConsentService consentService,
        IRequestContextService requestContextService,
        IHttpClientFactory clientFactory, 
        ILoggerFactory loggerFactory)
    {
        _availableEvidenceCodesService = availableEvidenceCodesService;
        _consentService = consentService;
        _requestContextService = requestContextService;
        _logger = loggerFactory.CreateLogger<EvidenceStatusService>();
        _clientFactory = clientFactory;
    }

    public async Task<EvidenceStatus> GetEvidenceStatusAsync(Accreditation accreditation, EvidenceCode evidenceCode, bool onlyLocalChecks)
    {
        EvidenceStatusCode status;
        var isConsentRequest = false;

        // Since evidencecodes in stored accreditation does not contain requirements, we need to rehydrate it from availableevidenceservice
        // This also validates that the evidence code requested is still available at the ES
        var stillAvailable = await TryRehydrateEvidenceCode(evidenceCode);
        if (!stillAvailable)
        {
            status = EvidenceStatusCode.Unavailable;
        }
        else if (_consentService.EvidenceCodeRequiresConsent(evidenceCode))
        {
            var consentStatus = await _consentService.Check(accreditation, onlyLocalChecks);
            status = MapConsentStatusToEvidenceStatusCode(consentStatus);
            isConsentRequest = true;
        }
        else
        {
            status = EvidenceStatusCode.Available;
        }

        // If the code is marked as asyncronous, we need to actually ask the ES itself about the status
        if (stillAvailable && evidenceCode.IsAsynchronous && !isConsentRequest)
        {
            if (onlyLocalChecks)
            {
                status = EvidenceStatusCode.AggregateUnknown;
            }
            else
            {
                using (var t = _logger.Timer($"{evidenceCode.EvidenceCodeName}-status"))
                {
                    _logger.LogInformation("Start get asyncronous evidence status code aid={accreditationId} evidence codename={evidenceCodeName}", accreditation.AccreditationId, evidenceCode.EvidenceCodeName);
                    status = await GetAsynchronousEvidenceStatusCode(accreditation, evidenceCode);
                    _logger.LogInformation("Completed get asyncronous evidence status code aid={accreditationId} evidence codename={evidenceCodeName} status={evidenceStatus} elapsedMs={elapsedMs}", accreditation.AccreditationId, evidenceCode.EvidenceCodeName, status, t.ElapsedMilliseconds);
                }
            }
        }

        return new EvidenceStatus
        {
            EvidenceCodeName = evidenceCode.EvidenceCodeName,
            Status = status,
            ValidFrom = accreditation.Issued,
            ValidTo = accreditation.ValidTo
        };
    }

    public async Task<List<EvidenceStatus>> GetEvidenceStatusListAsync(Accreditation accreditation)
    {
        var list = new List<EvidenceStatus>();

        using (var t = _logger.Timer("get-evidence-status-list"))
        {
            _logger.LogInformation("Start get evidence list aid={accreditationId}", accreditation.AccreditationId);
            foreach (var code in accreditation.EvidenceCodes)
            {
                list.Add(await GetEvidenceStatusAsync(accreditation, code, false));
            }

            _logger.LogInformation("Completed get evidence list aid={accreditationId} numevidence={numEvidence} evidence={evidence} elapsedMs={elapsedMs}", accreditation.AccreditationId,
                list.Count, string.Join(",", list.Select(x => x.EvidenceCodeName)), t.ElapsedMilliseconds);
        }

        return list;
    }

    public async Task DetermineAggregateStatus(List<Accreditation> accreditations, bool onlyLocalChecks = true)
    {
        foreach (var accreditation in accreditations)
        {
            await DetermineAggregateStatus(accreditation, onlyLocalChecks);
        }
    }

    private async Task<bool> TryRehydrateEvidenceCode(EvidenceCode evidenceCode)
    {
        var availableEvidenceCodes = await _availableEvidenceCodesService.GetAvailableEvidenceCodes();

        var availableEvidenceCode = availableEvidenceCodes.FirstOrDefault(x =>
            x.EvidenceCodeName == evidenceCode.EvidenceCodeName);

        if (availableEvidenceCode == null)
        {
            // The evidence code is no longer available
            return false;
        }

        evidenceCode.AuthorizationRequirements = availableEvidenceCode.AuthorizationRequirements;

        // In case the evidence code has been move to a new source, we need to update the evidence code EvidenceSource
        // to reflect that of the availableEvidenceCode, so that requests are routed correctly
        evidenceCode.EvidenceSource = availableEvidenceCode.EvidenceSource;

        evidenceCode.AuthorizationRequirements = evidenceCode.AuthorizationRequirements.Where(
            x => x.AppliesToServiceContext.Count == 0 || x.AppliesToServiceContext.Contains(_requestContextService.ServiceContext.Name)).ToList();

        return true;
    }

    private async Task DetermineAggregateStatus(Accreditation accreditation, bool onlyLocalChecks = false)
    {
        foreach (var evidenceCode in accreditation.EvidenceCodes)
        {
            var evidenceStatus = await GetEvidenceStatusAsync(accreditation, evidenceCode, onlyLocalChecks);
            if (evidenceStatus.Status == EvidenceStatusCode.Available) continue;
            accreditation.AggregateStatus = evidenceStatus.Status;
            return;
        }
            
        accreditation.AggregateStatus = EvidenceStatusCode.Available;
    }

    private async Task<EvidenceStatusCode> GetAsynchronousEvidenceStatusCode(Accreditation accreditation, EvidenceCode evidenceCode)
    {
        var url = evidenceCode.GetEvidenceSourceUrl();

        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(30));


        var evidenceHarvesterRequest = new EvidenceHarvesterRequest
        {
            OrganizationNumber = accreditation.Subject,
            Requestor = accreditation.Requestor,
            SubjectParty = accreditation.SubjectParty,
            RequestorParty = accreditation.RequestorParty,
            ServiceContext = accreditation.ServiceContext,
            EvidenceCodeName = evidenceCode.EvidenceCodeName,
            AccreditationId = accreditation.AccreditationId,
            AsyncEvidenceCodeAction = AsyncEvidenceCodeAction.CheckStatus
        };

        request.JsonContent(evidenceHarvesterRequest);
        var client = _clientFactory.CreateClient("SafeHttpClient");

        return (await EvidenceSourceHelper.DoRequest<EvidenceStatusCode>(
            request,
            () => client.SendAsync(request, cts.Token)))!;
    }


    private static EvidenceStatusCode MapConsentStatusToEvidenceStatusCode(ConsentStatus consentStatus)
    {
        switch (consentStatus)
        {
            case ConsentStatus.Pending:
                return EvidenceStatusCode.PendingConsent;
            case ConsentStatus.Granted:
                return EvidenceStatusCode.Available;
            case ConsentStatus.Denied:
                return EvidenceStatusCode.Denied;
            case ConsentStatus.Expired:
                return EvidenceStatusCode.Expired;
            case ConsentStatus.Revoked:
                return EvidenceStatusCode.Denied;
            default:
                throw new ArgumentOutOfRangeException($"Unhandled ConsentStatus value: {consentStatus}");
        }
    }
}
