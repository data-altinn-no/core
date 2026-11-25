using Dan.Common.Models;
using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAccreditationRepository
{
    Task<Accreditation?> GetAccreditationAsync(string accreditationId, string? partitionKeyValue);
    Task<List<Accreditation>> QueryAccreditationsAsync(AccreditationsQuery accreditationsQuery, string? partitionKeyValue);

    /// <summary>
    /// Gets the accreditations across all owners that have an Altinn 3 consent request without a stored answer. The
    /// filter on validity is coarse; accreditations that expired within the last day may be included.
    /// </summary>
    Task<List<Accreditation>> GetAccreditationsPendingConsentAsync();
    Task<Accreditation> CreateAccreditationAsync(Accreditation accreditation);
    Task<bool> UpdateAccreditationAsync(Accreditation accreditation);

    /// <summary>
    /// Gets the accreditation together with the version it has, for use with <see cref="TryUpdateAccreditationAsync"/>
    /// </summary>
    /// <returns>Null if the accreditation does not exist</returns>
    Task<VersionedAccreditation?> GetAccreditationWithVersionAsync(string accreditationId, string partitionKeyValue);

    /// <summary>
    /// Saves the accreditation only if it still has the given version. <see cref="UpdateAccreditationAsync"/> replaces
    /// the document whatever has happened to it since it was read.
    /// </summary>
    /// <param name="accreditation">The accreditation to save</param>
    /// <param name="etag">The version from <see cref="GetAccreditationWithVersionAsync"/></param>
    /// <returns>False, without saving, if the accreditation has been changed or deleted since it was read</returns>
    Task<bool> TryUpdateAccreditationAsync(Accreditation accreditation, string etag);
    Task<bool> DeleteAccreditationAsync(Accreditation accreditation);
}