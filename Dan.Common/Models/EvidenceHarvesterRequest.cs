namespace Dan.Common.Models;

/// <summary>
/// Only used internally by Core and ES
/// </summary>
[DataContract]
public class EvidenceHarvesterRequest
{
    /// <summary>
    /// The norwegian organization number that we're requesting information about. If not set, see SubjectParty for other party types (persons, foreign companies)
    /// </summary>
    [Required]
    [DataMember(Name = "organizationNumber")]
    public string OrganizationNumber { get; set; }

    /// <summary>
    /// Asynchronous evidence codes also require the accreditation id to maintain state for a particular call
    /// </summary>
    [DataMember(Name = "accreditationId")]
    public string AccreditationId { get; set; }

    /// <summary>
    /// The norwegian organization number requesting the data about NorwegianOrganizationNumber. If not set, see RequestorParty for other party types (persons, foreign companies)
    /// </summary>
    [DataMember(Name = "requestor")]
    public string Requestor { get; set; }

    /// <summary>
    /// The party of which information is requested. This contains additional information about the party not present in "NorwegianOrganizationNumber"
    /// </summary>
    [DataMember(Name = "subjectParty")]
    public Party SubjectParty { get; set; }

    /// <summary>
    /// The party requesting information. This contains additional information about the party not present in "Requestor"
    /// </summary>
    [DataMember(Name = "requestorParty")]
    public Party RequestorParty { get; set; }

    /// <summary>
    /// For asynchronous evidence codes for initializing and checking status for asynchronous evidence codes. 
    /// Should be omitted for normal lookup-based evidence codes.
    /// </summary>
    [DataMember(Name = "asyncEvidenceCodeAction")]
    public AsyncEvidenceCodeAction? AsyncEvidenceCodeAction { get; set; }

    /// <summary>
    /// The service context associated with the harvest request
    /// </summary>
    [DataMember(Name = "serviceContext")]
    public string ServiceContext { get; set; }

    /// <summary>
    /// The evidence code that we're asking for
    /// </summary>
    [DataMember(Name = "evidenceCodeName")]
    public string EvidenceCodeName { get; set; }

    /// <summary>
    /// The evidence code parameters
    /// </summary>
    [DataMember(Name = "parameters")]
    public List<EvidenceParameter> Parameters { get; set; }

    /// <summary>
    /// For consent based codes, a self-contained JWT is required
    /// </summary>
    [DataMember(Name = "jwt")]
    public string JWT { get; set; }

    /// <summary>
    /// The Maskinporten token to be used in the ES against the source
    /// </summary>
    [DataMember(Name = "mpToken")]
    public string MPToken { get; set; }
}