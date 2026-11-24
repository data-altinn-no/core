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
    Task<bool> DeleteAccreditationAsync(Accreditation accreditation);
}