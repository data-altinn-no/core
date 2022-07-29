namespace Dan.Common.Interfaces;

/// <summary>
/// Describes all the available evidence codes for this source
/// </summary>
public interface IEvidenceSourceMetadata
{
    /// <summary>
    /// Gets the list of evidence codes implemented by this evidence source
    /// </summary>
    /// <returns>
    /// A list of the evidence codes
    /// </returns>
    List<EvidenceCode> GetEvidenceCodes();
}