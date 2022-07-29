namespace Dan.Common.Models;

/// <summary>
/// The authorization model that must be supplied to that /authorization REST endpoint
/// </summary>
[DataContract]
public class AuthorizationRequest
{
    /// <summary>
    /// The party requesting the evidence
    /// </summary>
    [DataMember(Name = "requestor")]
    public string Requestor { get; set; }

    /// <summary>
    /// The party the evidence is requested for
    /// </summary>
    [DataMember(Name = "subject")]
    public string Subject { get; set; }

    /// <summary>
    /// The party of which information is requested. This contains additional information about the party not present in "Subject"
    /// </summary>
    [DataMember(Name = "subjectParty")]
    public Party SubjectParty { get; set; }

    /// <summary>
    /// The party requesting information. This contains additional information about the party not present in "Requestor"
    /// </summary>
    [DataMember(Name = "requestorParty")]
    public Party RequestorParty { get; set; }

    /// <summary>
    /// The requested evidence
    /// </summary>
    [DataMember(Name = "evidenceRequests")]
    public List<EvidenceRequest> EvidenceRequests { get; set; }

    /// <summary>
    /// List of legal basis proving legal authority for the requested evidence
    /// </summary>
    [DataMember(Name = "legalBasisList")]
    public List<LegalBasis> LegalBasisList { get; set; }

    /// <summary>
    /// How long the accreditation should be valid. Also used for duration of consent (date part only).
    /// </summary>
    [DataMember(Name = "validTo")]
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// TED reference number, if applicable
    /// </summary>
    [DataMember(Name = "consentReference")]
    public string ConsentReference { get; set; }

    /// <summary>
    /// Arbitrary reference which will be saved with the Accreditation
    /// </summary>
    [DataMember(Name = "externalReference")]
    public string ExternalReference { get; set; }

    [DataMember(Name = "languageCode")]
    public string LanguageCode { get; set; }

    [DataMember(Name = "consentReceiptRedirectUrl")]
    public string ConsentReceiptRedirectUrl { get; set; }

    [DataMember(Name = "skipAltinnNotification")]
    public bool SkipAltinnNotification { get; set; }
}