using Dan.Common.Models;
using Dan.Core.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAccreditationRepository
{
    Task<Accreditation?> GetAccreditationAsync(string accreditationId, IRequestContextService requestContextService, bool allowExpired = false);
    Task<Accreditation?> GetAccreditationAsync(string accreditationId, bool allowExpired = false);
    Task<List<Accreditation>> QueryAccreditationsAsync(AccreditationsQuery accreditationsQuery, IRequestContextService requestContextService);
    Task<Accreditation> CreateAccreditationAsync(Accreditation accreditation);
    Task<bool> UpdateAccreditationAsync(Accreditation accreditation);
    Task<bool> DeleteAccreditationAsync(string accreditationId);
}