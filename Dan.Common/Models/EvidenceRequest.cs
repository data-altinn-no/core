namespace Dan.Common.Models;

/// <summary>
/// Evidence Request as part of an Authorization model
/// </summary>
[DataContract]
public class EvidenceRequest
{
    /// <summary>
    /// The evidence code requested
    /// </summary>
    [DataMember(Name = "evidenceCodeName")]
    public string EvidenceCodeName { get; set; }

    /// <summary>
    /// Supplied parameters
    /// </summary>
    [DataMember(Name = "parameters")]
    public List<EvidenceParameter> Parameters { get; set; }

    /// <summary>
    /// If a legal basis is supplied, its identifier goes here
    /// </summary>
    [DataMember(Name = "legalBasisId")]
    public string LegalBasisId { get; set; }

    /// <summary>
    /// If a legal basis is supplied, the reference within it may be supplied here if applicable
    /// </summary>
    [DataMember(Name = "legalBasisReference")]
    public string LegalBasisReference { get; set; }

    /// <summary>
    /// Whether a request for non-open evidence not covered by legal basis should result in a consent request being initiated
    /// </summary>
    [DataMember(Name = "requestConsent")]
    public bool? RequestConsent { get; set; }
}