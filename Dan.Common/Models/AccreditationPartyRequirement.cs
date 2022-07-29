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

    public AccreditationPartyRequirement()
    {
        PartyRequirements = new List<AccreditationPartyRequirementType>();
    }
}