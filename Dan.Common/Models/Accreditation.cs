using Dan.Common.Attributes;

namespace Dan.Common.Models;

/// <summary>
/// The accreditation returned from a successful authorization
/// </summary>
[DataContract]
public class Accreditation
{
    /// <summary>
    /// Gets or sets the identifier for the accreditation
    /// </summary>
    [DataMember(Name = "id")]
    public string AccreditationId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the party requesting the evidence
    /// </summary>
    [DataMember(Name = "requestor")]
    public string? Requestor { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the party the evidence is requested for
    /// </summary>
    [Required]
    [DataMember(Name = "subject")]
    public string? Subject { get; set; } = string.Empty;

    /// <summary>
    /// The party of which information is requested. This contains additional information about the party not present in "Subject"
    /// </summary>
    [DataMember(Name = "subjectParty")]
    public Party SubjectParty { get; set; } = new();

    /// <summary>
    /// The party requesting information. This contains additional information about the party not present in "Requestor"
    /// </summary>
    [DataMember(Name = "requestorParty")]
    public Party RequestorParty { get; set; } = new();

    /// <summary>
    /// Gets or sets the aggregate status for all evidence codes in the accreditation, excluding asynchronous evidence codes.
    /// </summary>
    [Required]
    [DataMember(Name = "aggregateStatus")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public EvidenceStatusCode AggregateStatus { get; set; } = EvidenceStatusCode.Unknown;

    /// <summary>
    /// Flag to show if the accreditation is made from a direct harvest request
    /// </summary>
    [DataMember(Name = "isDirectHarvest")]
    public bool IsDirectHarvest { get; set; }

    /// <summary>
    /// Gets or sets a list of evidence codes associated with the accreditation. Only supplied when requesting a single accreditation.
    /// </summary>
    [DataMember(Name = "evidenceCodes")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<EvidenceCode> EvidenceCodes { get; set; } = new();

    /// <summary>
    /// Gets or sets a dict of skipped evidence codes because of failed soft authorization requirement. Only first failed soft requirement is included.
    /// </summary>
    [DataMember(Name = "skippedEvidenceCodes")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public Dictionary<string, Requirement> SkippedEvidenceCodes { get; set; } = new();

    /// <summary>
    /// Gets or sets when the accreditation was created
    /// </summary>
    [Required]
    [DataMember(Name = "issued")]
    public DateTime Issued { get; set; }

    /// <summary>
    /// Gets or sets when the accreditation was last changed. Usually means the time at which an consent request was answered.
    /// </summary>
    [Required]
    [DataMember(Name = "lastChanged")]
    public DateTime LastChanged { get; set; }

    /// <summary>
    /// Gets or sets how long the accreditation is valid
    /// </summary>
    [Required]
    [DataMember(Name = "validTo")]
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Gets or sets TED reference
    /// </summary>
    [DataMember(Name = "consentReference")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ConsentReference { get; set; }


    /// <summary>
    /// Gets or sets arbitrary reference provided in the authorization call
    /// </summary>
    [DataMember(Name = "externalReference")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// Gets or sets the authorization code used with consents
    /// </summary>
    [DataMember(Name = "authorizationCode")]
    [Hidden]
    public string? AuthorizationCode { get; set; }

    /// <summary>
    /// Gets or sets the owner (organization number) for this accreditation. Matched against enterprise certificates.
    /// </summary>
    [DataMember(Name = "Owner")]
    [Hidden]
    public string? Owner { get; set; }

    /// <summary>
    /// The selected language for the accreditation, used for consent request texts and notifications, no-nb, no-nn or en
    /// </summary>
    [DataMember(Name = "languageCode")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? LanguageCode { get; set; }


    /// <summary>
    /// Gets or sets a list of reminders sent to the subject, used for reminding subjects to consent
    /// </summary>
    [DataMember(Name = "reminders")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [Hidden]
    public List<NotificationReminder> Reminders { get; set; } = new();


    /// <summary>
    /// A list of timestampss and data set names
    /// </summary>
    [DataMember(Name = "dataRetrievals")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [Hidden]
    public List<DataRetrieval> DataRetrievals { get; set; } = new();

    /// <summary>
    /// A list of timestampss and data set names
    /// </summary>
    [DataMember(Name = "serviceContext")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ServiceContext { get; set; }

    /// <summary>
    /// URL for redirect from funcconsentreceipt if user is in GUI guided process
    /// </summary>
    [DataMember(Name = "consentReceiptRedirectUrl")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ConsentReceiptRedirectUrl { get; set; }

    /// <summary>
    /// A link to the altinn consent page for this process, used to redirect users back to GUI guided process
    /// </summary>
    [DataMember(Name = "altinnConsentUrl")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? AltinnConsentUrl { get; set; }

    /// <summary>
    /// The id of the consent request in Altinn 3, replaces the authorization code used in Altinn 2. Placed in separate field to make the distinction easier.
    /// </summary>
    [DataMember(Name = "altinn3ConsentId")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Altinn3ConsentId { get; set; }
}