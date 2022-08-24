namespace Dan.Common.Models;

/// <summary>
/// Model for holding the legal basis (ESPD)
/// </summary>
[DataContract]
public class LegalBasis
{
    /// <summary>
    /// The content for the legal basis, usually the ESPD XML
    /// </summary>
    [Required]
    [DataMember(Name = "content")]
    public string? Content { get; set; }

    /// <summary>
    /// Gets the arbitrary identifier for the legal basis, used to reference this from evidence requests
    /// </summary>
    [Required]
    [DataMember(Name = "id")]
    public string? Id { get; set; }

    /// <summary>
    /// The type of legal basis, usually ESPD
    /// </summary>
    [Required]
    [DataMember(Name = "type")]
    [JsonConverter(typeof(StringEnumConverter))]
    public LegalBasisType? Type { get; set; }
}