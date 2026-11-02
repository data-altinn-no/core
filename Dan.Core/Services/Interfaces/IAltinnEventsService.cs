using Dan.Core.Models.Events;

namespace Dan.Core.Services.Interfaces;

/// <summary>
/// Publishes CloudEvents to the Altinn 3 Events API.
/// </summary>
public interface IAltinnEventsService
{
    /// <summary>
    /// Publishes a single CloudEvent. Requires that the resource named in the event is registered in the
    /// Altinn Resource Registry with a policy granting data.altinn.no the "publish" action.
    /// </summary>
    /// <param name="cloudEvent">The event to publish</param>
    /// <param name="idempotencyKey">Idempotency-Key header value (a GUID); Altinn de-duplicates on it</param>
    /// <exception cref="Dan.Core.Exceptions.ServiceNotAvailableException">If the token could not be retrieved or Altinn returned an error</exception>
    Task Publish(AltinnCloudEvent cloudEvent, string idempotencyKey);
}
