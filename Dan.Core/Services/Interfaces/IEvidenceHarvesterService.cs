using Dan.Common.Models;

namespace Dan.Core.Services.Interfaces;

public interface IEvidenceHarvesterService
{
    Task<Evidence> Harvest(string evidenceCodeName, Accreditation accreditation, EvidenceHarvesterOptions? evidenceHarvesterOptions = default);
    Task<Stream> HarvestStream(string evidenceCodeName, Accreditation accreditation, EvidenceHarvesterOptions? evidenceHarvesterOptions = default);
    Task<Evidence> HarvestOpenData(EvidenceCode evidenceCodeName, string identifier = "");
}
