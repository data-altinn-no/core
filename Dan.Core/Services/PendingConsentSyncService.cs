using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Services;

public class PendingConsentSyncService : IPendingConsentSyncService
{
    private readonly IAccreditationRepository _accreditationRepository;
    private readonly IAltinn3ConsentService _consentService;
    private readonly ILogger<PendingConsentSyncService> _logger;

    public PendingConsentSyncService(
        IAccreditationRepository accreditationRepository,
        IAltinn3ConsentService consentService,
        ILoggerFactory loggerFactory)
    {
        _accreditationRepository = accreditationRepository;
        _consentService = consentService;
        _logger = loggerFactory.CreateLogger<PendingConsentSyncService>();
    }

    /// <inheritdoc />
    public async Task<PendingConsentSyncResult> Sync()
    {
        var pending = await _accreditationRepository.GetAccreditationsPendingConsentAsync();

        int checkedCount = 0, granted = 0, denied = 0, failed = 0;
        foreach (var accreditation in pending.Where(IsAwaitingConsent))
        {
            checkedCount++;
            try
            {
                var newStatus = await GetAnswerFromAltinn(accreditation);
                if (newStatus == null || !await StoreAnswer(accreditation, newStatus))
                {
                    continue;
                }

                if (newStatus == Altinn3ConsentService.ConsentDenied)
                {
                    denied++;
                }
                else
                {
                    granted++;
                }
            }
            catch (Exception ex)
            {
                // One accreditation failing must not stop the rest; it is picked up again on the next run
                failed++;
                _logger.LogError(ex, "Failed syncing pending consent aid={accreditationId} consentRequestId={consentRequestId}",
                    accreditation.AccreditationId, accreditation.Altinn3ConsentId);
            }
        }

        return new PendingConsentSyncResult(checkedCount, granted, denied, failed);
    }

    /// <summary>
    /// The repository query is coarse on validity, and the answer may have been stored since the query ran
    /// </summary>
    private static bool IsAwaitingConsent(Accreditation accreditation)
    {
        return accreditation.ValidTo > DateTime.Now
               && !string.IsNullOrEmpty(accreditation.Altinn3ConsentId)
               && string.IsNullOrEmpty(accreditation.Altinn3ConsentStatus);
    }

    /// <summary>
    /// Returns the consent status to store, or null if the consent request has no final answer to store. A rejected
    /// request can never be accepted later, and a consent that was given and then revoked is stored as denied.
    /// </summary>
    private async Task<string?> GetAnswerFromAltinn(Accreditation accreditation)
    {
        var status = await _consentService.GetConsentRequestStatus(accreditation.Altinn3ConsentId!);

        return status switch
        {
            Altinn3ConsentRequestStatus.Accepted => Altinn3ConsentService.ConsentGranted,
            Altinn3ConsentRequestStatus.Rejected => Altinn3ConsentService.ConsentDenied,
            Altinn3ConsentRequestStatus.Revoked => Altinn3ConsentService.ConsentDenied,
            _ => null
        };
    }

    /// <summary>
    /// Stores the answer on a freshly read copy of the accreditation. The update replaces the whole document, so
    /// saving the copy read at the start of the run could overwrite a reminder or a consent receipt written since.
    /// </summary>
    /// <returns>True if the answer was stored</returns>
    private async Task<bool> StoreAnswer(Accreditation accreditation, string newStatus)
    {
        var current = await _accreditationRepository.GetAccreditationAsync(accreditation.AccreditationId, accreditation.Owner);
        if (current == null || !string.IsNullOrEmpty(current.Altinn3ConsentStatus))
        {
            return false;
        }

        current.Altinn3ConsentStatus = newStatus;
        current.LastChanged = DateTime.Now;
        await _accreditationRepository.UpdateAccreditationAsync(current);

        _logger.DanLog(current, newStatus == Altinn3ConsentService.ConsentDenied ? LogAction.ConsentDenied : LogAction.ConsentGiven);
        return true;
    }
}
