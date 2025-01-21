namespace Dan.Common.Models;

/// <summary>
/// Accreditation party relation requirement
/// </summary>
[DataContract]
public class AccreditationPartyRequirement : Requirement
{
    /// <summary>
    /// Required relationships between accreditation parties
    /// </summary>
    [Required]
    [DataMember(Name = "partyRequirements")]
    [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
    public List<AccreditationPartyRequirementType> PartyRequirements { get; set; }

    /// <summary>
    /// Default constructor, sets PartyRequirements to a new empty list
    /// </summary>
    public AccreditationPartyRequirement()
    {
        PartyRequirements = new List<AccreditationPartyRequirementType>();
    }
}