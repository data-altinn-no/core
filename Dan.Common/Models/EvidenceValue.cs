namespace Dan.Common.Models;

/// <summary>
/// Describing the format and containing the value of an evidence
/// </summary>
[DataContract]
public class EvidenceValue : ICloneable
{
    /// <summary>
    /// If value type is attachment, this contains the MIME type (example: application/pdf)
    /// </summary>
    [DataMember(Name = "mimeType")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? MimeType { get; set; }

    /// <summary>
    /// A name describing the evidence value
    /// </summary>
    [Required]
    [DataMember(Name = "evidenceValueName")]
    public string EvidenceValueName { get; set; } = string.Empty;

    /// <summary>
    /// Arbitrary text describing the purpose and content of this specific field in the dataset
    /// </summary>
    [DataMember(Name = "description")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? Description { get; set; }

    /// <summary>
    /// The source from which the evidence is harvested
    /// </summary>
    [Required]
    [DataMember(Name = "source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The time of which the evidence was collected from the source, if used in context of an Evidence
    /// </summary>
    [DataMember(Name = "timestamp")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? Timestamp { get; set; }

    /// <summary>
    /// The value for the evidence, if used in context of an Evidence
    /// </summary>
    [DataMember(Name = "value")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public object? Value { get; set; }

    /// <summary>
    /// The format over the evidence value
    /// </summary>
    [Required]
    [DataMember(Name = "valueType")]
    [JsonConverter(typeof(StringEnumConverter))]
    public EvidenceValueType? ValueType { get; set; }

    /// <summary>
    /// If a richer type is required, a JSON Schema may be supplied
    /// </summary>
    [DataMember(Name = "jsonSchemaDefintion")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? JsonSchemaDefintion { get; set; }

    /// <inheritdoc />
    public object Clone()
    {
        return MemberwiseClone();
    }
}