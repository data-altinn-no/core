namespace Dan.Common.Models;

/// <summary>
/// Altinn Role requirement
/// </summary>
[DataContract]
public class AltinnRoleRequirement : Requirement
{
    /// <summary>
    /// The name of the required Altinn role
    /// </summary>
    [DataMember(Name = "roleCode")]
    [Required]
    public string? RoleCode { get; set; }

    /// <summary>
    /// Which party needs to delegate the role
    /// </summary>
    [DataMember(Name = "offeredBy")]
    [Required]
    [JsonConverter(typeof(StringEnumConverter))]
    public AccreditationPartyTypes OfferedBy { get; set; }

    /// <summary>
    /// Which party needs to receive the role
    /// </summary>
    [DataMember(Name = "coveredBy")]
    [Required]
    [JsonConverter(typeof(StringEnumConverter))]
    public AccreditationPartyTypes CoveredBy { get; set; }
}