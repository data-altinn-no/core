using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Extensions;
using Dan.Core.Models;
using Dan.Core.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Dan.Core.Services;

public class PendingConsentSyncService : IPendingConsentSyncService
{
    /// <summary>
    /// The run is stopped when this many accreditations in a row fail
    /// </summary>
    private const int MaxConsecutiveFailures = 10;

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
        var awaitingConsent = pending.Where(IsAwaitingConsent).ToList();

        int checkedCount = 0, granted = 0, denied = 0, failed = 0, consecutiveFailures = 0;
        foreach (var accreditation in awaitingConsent)
        {
            checkedCount++;
            try
            {
                var status = await _consentService.GetConsentRequestStatus(accreditation.Altinn3ConsentId!);
                if (status == Altinn3ConsentRequestStatus.NotFound)
                {
                    // Altinn answered, so this is not a sign that the rest of the run will fail
                    failed++;
                    consecutiveFailures = 0;
                    _logger.LogError("Failed syncing pending consent, as Altinn does not know the consent request aid={accreditationId} consentRequestId={consentRequestId}",
                        accreditation.AccreditationId, accreditation.Altinn3ConsentId);
                    continue;
                }

                var newStatus = GetStatusToStore(status);
                var stored = newStatus != null && await StoreAnswer(accreditation, newStatus);
                consecutiveFailures = 0;
                if (!stored)
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

                // Many in a row means Altinn or the database is unavailable. Going on would mean waiting for a timeout
                // and logging an error for each of the rest.
                if (++consecutiveFailures >= MaxConsecutiveFailures)
                {
                    _logger.LogError("Stopped the pending consent sync after {consecutiveFailures} failures in a row, notChecked={notChecked}",
                        consecutiveFailures, awaitingConsent.Count - checkedCount);
                    break;
                }
            }
        }

        return new PendingConsentSyncResult(checkedCount, granted, denied, failed, awaitingConsent.Count - checkedCount);
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
    private static string? GetStatusToStore(Altinn3ConsentRequestStatus status)
    {
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
    /// The save is conditional on the version that was read, so a change made between the read and the save is not
    /// overwritten either.
    /// </summary>
    /// <returns>True if the answer was stored</returns>
    private async Task<bool> StoreAnswer(Accreditation accreditation, string newStatus)
    {
        // Owner is the partition key, so it is set on everything read from the container
        var versioned = await _accreditationRepository.GetAccreditationWithVersionAsync(accreditation.AccreditationId, accreditation.Owner!);
        if (versioned == null || !string.IsNullOrEmpty(versioned.Accreditation.Altinn3ConsentStatus))
        {
            return false;
        }

        var current = versioned.Accreditation;
        current.Altinn3ConsentStatus = newStatus;
        current.LastChanged = DateTime.Now;
        if (!await _accreditationRepository.TryUpdateAccreditationAsync(current, versioned.ETag))
        {
            // Not a failure; if the consent is still unanswered in DAN, the next run stores it
            _logger.LogInformation("Did not store consent answer, as the accreditation was changed during the sync aid={accreditationId} consentRequestId={consentRequestId}",
                current.AccreditationId, current.Altinn3ConsentId);
            return false;
        }

        _logger.DanLog(current, newStatus == Altinn3ConsentService.ConsentDenied ? LogAction.ConsentDenied : LogAction.ConsentGiven);
        return true;
    }
}
