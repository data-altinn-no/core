using Dan.Common.Models;
using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAccreditationRepository
{
    Task<Accreditation?> GetAccreditationAsync(string accreditationId, string? partitionKeyValue);
    Task<List<Accreditation>> QueryAccreditationsAsync(AccreditationsQuery accreditationsQuery, string? partitionKeyValue);
    Task<Accreditation> CreateAccreditationAsync(Accreditation accreditation);
    Task<bool> UpdateAccreditationAsync(Accreditation accreditation);
    Task<bool> DeleteAccreditationAsync(Accreditation accreditation);

    /// <summary>
    /// Returns accreditations changed after the given time that have at least one consent event
    /// which is not yet published and has fewer than maxAttempts publish attempts.
    /// </summary>
    Task<List<Accreditation>> GetAccreditationsWithUnpublishedConsentEventsAsync(DateTime changedAfter, int maxAttempts);

    /// <summary>
    /// Returns accreditations with an unanswered Altinn 3 consent request whose validTo is between expiredAfter and now,
    /// and which do not yet have a consent event of the given type (i.e. expiry has not been recorded).
    /// </summary>
    Task<List<Accreditation>> GetAccreditationsWithExpiredPendingConsentAsync(DateTime now, DateTime expiredAfter, string expiredEventType);
}