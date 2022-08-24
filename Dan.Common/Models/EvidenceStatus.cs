namespace Dan.Common.Models;

/// <summary>
/// Evidence Request as part of an Authorization model
/// </summary>
[DataContract]
public class EvidenceStatus
{
    /// <summary>
    /// The name of the evidence code this status refers to
    /// </summary>
    [DataMember(Name = "evidenceCodeName")]
    public string? EvidenceCodeName { get; set; }

    /// <summary>
    /// Gets or Sets Status
    /// </summary>
    [DataMember(Name = "status")]
    public EvidenceStatusCode Status { get; set; } = new();

    /// <summary>
    /// From when the evidence code is available
    /// </summary>
    [DataMember(Name = "validFrom")]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Until when the evidence code is available
    /// </summary>
    [DataMember(Name = "validTo")]
    public DateTime? ValidTo { get; set; }
}