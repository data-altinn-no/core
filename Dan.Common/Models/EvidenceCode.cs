using Dan.Common.Attributes;

namespace Dan.Common.Models;

/// <summary>
/// Describing an EvidenceCode and what values it carries. When used in context of a Accreditation, also includes the timespan of which the evidence is available
/// </summary>
[DataContract]
public class EvidenceCode
{
    /// <summary>
    /// Name of the dataset
    /// </summary>
    [Required]
    [DataMember(Name = "evidenceCodeName")]
    public string EvidenceCodeName { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary text describing the purpose and content of the dataset
    /// </summary>
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// If the dataset has any parameters
    /// </summary>
    [DataMember(Name = "parameters")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<EvidenceParameter>? Parameters { get; set; }

    /// <summary>
    /// Whether or not the dataset has been flagged as representing data not available by simple lookup.
    /// This causes Core to perform an explicit call to the harvester function of the dataset to 
    /// initialize or check for status.
    /// </summary>
    [DataMember(Name = "isAsynchronous")]
    public bool IsAsynchronous { get; set; }

    /// <summary>
    /// If set, specifies the maximum amount of days an accreditation referring this dataset can be valid,
    /// which also affects the duration of consent delegations and thus token expiry.
    /// </summary>
    [DataMember(Name = "maxValidDays")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? MaxValidDays { get; set; }

    /// <summary>
    /// The values associated with this dataset
    /// </summary>
    [Required]
    [DataMember(Name = "values")]
    public List<EvidenceValue> Values { get; set; } = new();

    /// <summary>
    /// The plugin the dataset belongs to
    /// </summary>
    [DataMember(Name = "evidenceSource")]
    [Hidden]
    public string EvidenceSource { get; set; } = string.Empty;

    /// <summary>
    /// The Service Code
    /// </summary>
    [DataMember(Name = "serviceCode")]
    [Hidden]
    public string? ServiceCode { get; set; }

    /// <summary>
    /// The service edition code
    /// </summary>
    [DataMember(Name = "serviceEditionCode")]
    [Hidden]
    public int ServiceEditionCode { get; set; }

    /// <summary>
    /// A list of authorization requirements for the dataset, who, what, how
    /// </summary>
    [DataMember(Name = "authorizationRequirements")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<Requirement> AuthorizationRequirements { get; set; } = new();

    /// <summary>
    /// A space separated list of scopes to request when generating access tokens
    /// </summary>
    [DataMember(Name = "requiredScopes")]
    [Hidden]
    public string? RequiredScopes { get; set; }

    /// <summary>
    /// DEPRECATED: An identifier of the domain service to which the dataset belongs. Use BelongsToServiceContexts
    /// </summary>
    /// <see cref="BelongsToServiceContexts"/>
    [DataMember(Name = "serviceContext")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? ServiceContext { get; set; }

    /// <summary>
    /// A list of identifiers of the domain services to which the dataset belongs
    /// </summary>
    [DataMember(Name = "belongsToServiceContexts")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public List<string> BelongsToServiceContexts { get; set; } = new();

    /// <summary>
    /// Whether or not the evidence code has been flagged as open data.
    /// This allows it to be used without authentication, authorization and apikey
    /// </summary>
    [DataMember(Name = "isPublic")]
    public bool IsPublic { get; set; }

    /// <summary>
    /// Sets a date for when the dataset will no longer be valid.
    /// After this date the dataset will not be included in the metadata for a plugin
    /// </summary>
    [DataMember(Name = "validTo")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? ValidTo { get; set; }

    /// <summary>
    /// Specifies the date when the dataset becomes valid
    /// Prior to this date the dataset will not be included in the metadata for a plugin
    /// </summary>
    [DataMember(Name = "validFrom")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? ValidFrom { get; set; }

    /// <summary>
    /// Optional warning for datasets that will be removed in the future
    /// Should be used in combination with <see cref="ValidTo" />
    /// </summary>
    [DataMember(Name = "deprecationWarning")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? DeprecationWarning { get; set; }

    /// <summary>
    /// Optional setting for custom timeout (in seconds) in evidencecodes when harvesting data
    /// </summary>
    [DataMember(Name = "timeout")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public int? Timeout { get; set; }
}