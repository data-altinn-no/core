using Dan.Common.Models;
using Dan.Core.Models.Events;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Outbox-style publisher of consent events (granted/denied/expired) to Altinn Events. Records are kept on the
/// accreditation (<see cref="Accreditation.ConsentEvents"/>); callers persist the accreditation, this service
/// only mutates it. This keeps publishing at-least-once: persist the record, try to publish, let the timer retry.
/// </summary>
public interface IConsentEventPublisher
{
    /// <summary>
    /// Whether consent event publishing is enabled (feature flag). When false, Enqueue and TryPublishPending are no-ops.
    /// </summary>
    bool IsEnabled { get; }

    /// <summary>
    /// Adds an unpublished consent event record of the given type to the accreditation unless one already exists.
    /// Does not persist the accreditation.
    /// </summary>
    /// <returns>True if a record was added</returns>
    bool Enqueue(Accreditation accreditation, string eventType);

    /// <summary>
    /// Attempts to publish every unpublished record on the accreditation that has not exceeded the max attempts.
    /// Updates the records in place. Never throws.
    /// </summary>
    /// <returns>True if any record was changed and the accreditation should be persisted</returns>
    Task<bool> TryPublishPending(Accreditation accreditation);

    /// <summary>
    /// Builds the CloudEvent for a record. Subject is the accreditation owner (the client org), never the data subject.
    /// </summary>
    AltinnCloudEvent BuildEvent(Accreditation accreditation, ConsentEventRecord record);
}
