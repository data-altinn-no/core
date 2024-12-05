using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IAvailableEvidenceCodesService
{
    public Task<List<EvidenceCode>> GetAvailableEvidenceCodes(bool forceRefresh = false);

    public Dictionary<string, string> GetAliases();
}
