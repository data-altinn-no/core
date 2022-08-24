using Dan.Common.Enums;
using Dan.Common.Helpers.Util;
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
    private readonly ILogger<EvidenceStatusService> _logger;
    private readonly IHttpClientFactory _clientFactory;

    public EvidenceStatusService(IAvailableEvidenceCodesService availableEvidenceCodesService, IConsentService consentService, IHttpClientFactory clientFactory, ILoggerFactory loggerFactory)
    {
        _availableEvidenceCodesService = availableEvidenceCodesService;
        _consentService = consentService;
        _logger = loggerFactory.CreateLogger<EvidenceStatusService>();
        _clientFactory = clientFactory;
    }

    public async Task<EvidenceStatus> GetEvidenceStatusAsync(Accreditation accreditation, EvidenceCode requestedEvidenceCode, bool onlyLocalChecks)
    {
        EvidenceStatusCode status;
        var isConsentRequest = false;

        // Since evidencecodes in stored accreditation does not contain requirements, we need to get it from availableevidenceservice
        // This also validates that the evidence code requested is still available at the ES
        var availableEvidenceCodes = await _availableEvidenceCodesService.GetAvailableEvidenceCodes();
        var evidenceCode = availableEvidenceCodes.FirstOrDefault(x =>
            x.EvidenceCodeName == requestedEvidenceCode.EvidenceCodeName);

        if (evidenceCode == null)
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
        if (evidenceCode != null && evidenceCode.IsAsynchronous && !isConsentRequest)
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

        _logger.LogInformation("Returning evidence status aid={accreditationId} codename={evidenceCodeName} status={evidenceStatus} valid from={validFrom} valid to={validTo}", accreditation.AccreditationId, requestedEvidenceCode.EvidenceCodeName, status.Code, accreditation.Issued, accreditation.ValidTo);
        return new EvidenceStatus
        {
            EvidenceCodeName = requestedEvidenceCode.EvidenceCodeName,
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
