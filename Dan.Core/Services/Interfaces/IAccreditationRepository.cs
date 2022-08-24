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
}