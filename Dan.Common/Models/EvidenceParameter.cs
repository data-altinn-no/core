namespace Dan.Common.Models;

/// <summary>
/// Describing the format and containing the value of an evidence
/// </summary>
[DataContract]
public class EvidenceParameter
{
    /// <summary>
    /// A name describing the evidence parameter
    /// </summary>
    [Required]
    [DataMember(Name = "evidenceParamName")]
    public string? EvidenceParamName { get; set; }

    /// <summary>
    /// The format of the evidence parameter
    /// </summary>
    [DataMember(Name = "paramType")]
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public EvidenceParamType? ParamType { get; set; }

    /// <summary>
    /// Whether or not the evidence parameter is required, if used in context of a evidence code description
    /// </summary>
    [DataMember(Name = "required")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public bool? Required { get; set; }

    /// <summary>
    /// The value for the evidence parameter, if used in context of a evidence code request
    /// </summary>
    [DataMember(Name = "value")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Value { get; set; }
}