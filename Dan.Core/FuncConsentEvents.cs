using Dan.Common;
using Dan.Common.Enums;
using Dan.Common.Models;
using Dan.Core.Config;
using Dan.Core.Extensions;
using Dan.Core.Services.Interfaces;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace Dan.Core;

/// <summary>
/// Timer-driven maintenance for consent events published to Altinn Events:
/// retries unpublished events and detects consent requests that expired unanswered.
/// </summary>
public class FuncConsentEvents
{
    private readonly IAccreditationRepository _accreditationRepository;
    private readonly IConsentEventPublisher _consentEventPublisher;
    private readonly ILogger<FuncConsentEvents> _logger;

    public FuncConsentEvents(
        IAccreditationRepository accreditationRepository,
        IConsentEventPublisher consentEventPublisher,
        ILoggerFactory loggerFactory)
    {
        _accreditationRepository = accreditationRepository;
        _consentEventPublisher = consentEventPublisher;
        _logger = loggerFactory.CreateLogger<FuncConsentEvents>();
    }

#if !DEBUG
    /// <summary>
    /// Runs every five minutes. Timer triggers are singletons per function app, so runs never overlap.
    /// </summary>
    [Function("ConsentEventsProcessor")]
    public async Task Run([TimerTrigger("0 */5 * * * *")] TimerInfo timerInfo)
    {
        await ProcessAsync();
    }
#endif

    /// <summary>
    /// Retries unpublished consent events and records/publishes expiry of unanswered consent requests.
    /// Separated from the timer entry point to be testable.
    /// </summary>
    /// <returns>Number of accreditations that were updated</returns>
    public async Task<int> ProcessAsync()
    {
        if (!_consentEventPublisher.IsEnabled)
        {
            return 0;
        }

        var updated = 0;
        updated += await RetryUnpublishedAsync();
        updated += await RecordExpiredConsentsAsync();

        _logger.LogInformation("Consent events processor finished, updated {count} accreditations", updated);
        return updated;
    }

    private async Task<int> RetryUnpublishedAsync()
    {
        var changedAfter = DateTime.Now.AddDays(-Settings.ConsentEventsLookbackDays);
        var accreditations = await _accreditationRepository.GetAccreditationsWithUnpublishedConsentEventsAsync(
            changedAfter, Settings.ConsentEventsMaxAttempts);

        var updated = 0;
        foreach (var accreditation in accreditations)
        {
            if (await _consentEventPublisher.TryPublishPending(accreditation))
            {
                await _accreditationRepository.UpdateAccreditationAsync(accreditation);
                updated++;
            }
        }

        _logger.LogInformation("Consent event retry: {pending} accreditations with unpublished events, {updated} updated",
            accreditations.Count, updated);
        return updated;
    }

    private async Task<int> RecordExpiredConsentsAsync()
    {
        var now = DateTime.Now;
        var expiredAfter = now.AddDays(-Settings.ConsentEventsLookbackDays);
        var accreditations = await _accreditationRepository.GetAccreditationsWithExpiredPendingConsentAsync(
            now, expiredAfter, ConsentEventTypes.Expired);

        var updated = 0;
        foreach (var accreditation in accreditations)
        {
            if (!_consentEventPublisher.Enqueue(accreditation, ConsentEventTypes.Expired))
            {
                continue;
            }

            // Persist the record before the first publish attempt so a crash here is retried, not lost
            await _accreditationRepository.UpdateAccreditationAsync(accreditation);
            _logger.DanLog(accreditation, LogAction.ConsentExpired);
            updated++;

            if (await _consentEventPublisher.TryPublishPending(accreditation))
            {
                await _accreditationRepository.UpdateAccreditationAsync(accreditation);
            }
        }

        _logger.LogInformation("Consent expiry sweep: {expired} newly expired consent requests recorded", updated);
        return updated;
    }
}
