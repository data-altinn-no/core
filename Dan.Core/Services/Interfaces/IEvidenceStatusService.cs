using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IEvidenceStatusService
{
    public Task<EvidenceStatus> GetEvidenceStatusAsync(Accreditation accreditation, EvidenceCode requestedEvidenceCode, bool onlyLocalChecks);
    public Task<List<EvidenceStatus>> GetEvidenceStatusListAsync(Accreditation accreditation);
}
