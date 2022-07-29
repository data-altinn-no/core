namespace Dan.Common.Models;

/// <summary>
/// Party type requirement
/// </summary>
[DataContract]
public class PartyTypeRequirement : Requirement
{
    /// <summary>
    /// A list of allowed party types for each accreditation role
    /// </summary>
    [DataMember(Name = "allowedPartyTypes")]
    [Required]
    public AllowedPartyTypesList AllowedPartyTypes { get; set; }

    public PartyTypeRequirement()
    {
        AllowedPartyTypes = new AllowedPartyTypesList();
    }
}