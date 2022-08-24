namespace Dan.Common.Models;

/// <summary>
/// Represents the actual evidence as harvested from one or more sources
/// </summary>
[DataContract]
public class Evidence
{
    /// <summary>
    /// Gets or Sets EvidenceStatus
    /// </summary>
    [DataMember(Name = "evidenceStatus")]
    public EvidenceStatus EvidenceStatus { get; set; } = new();

    /// <summary>
    /// The evidence payloads
    /// </summary>
    [DataMember(Name = "evidenceValues")]
    public List<EvidenceValue> EvidenceValues { get; set; } = new();
}