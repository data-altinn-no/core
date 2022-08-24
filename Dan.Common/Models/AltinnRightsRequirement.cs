namespace Dan.Common.Models;

/// <summary>
/// Altinn Rights requirement
/// </summary>
[DataContract]
public class AltinnRightsRequirement : Requirement
{
    /// <summary>
    /// Service code for the Altinn service
    /// </summary>
    [DataMember(Name = "serviceCode")]
    [Required]
    public string? ServiceCode { get; set; }

    /// <summary>
    /// Service edition for the Altinn service
    /// </summary>
    [DataMember(Name = "serviceEdition")]
    [Required]
    public string? ServiceEdition { get; set; }

    /// <summary>
    /// Which party needs to delegate the rights
    /// </summary>
    [DataMember(Name = "offeredBy")]
    [Required]
    [JsonConverter(typeof(StringEnumConverter))]
    public AccreditationPartyTypes OfferedBy { get; set; }

    /// <summary>
    /// Which party needs to receive the rights
    /// </summary>
    [DataMember(Name = "coveredBy")]
    [Required]
    [JsonConverter(typeof(StringEnumConverter))]
    public AccreditationPartyTypes CoveredBy { get; set; }

    /// <summary>
    /// Required rights 
    /// </summary>
    [DataMember(Name = "actions")]
    [Required]
    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<AltinnAction> RightsActions { get; set; } = new();
}