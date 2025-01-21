namespace Dan.Common.Models;

/// <summary>
/// Whitelist requirement
/// </summary>
[DataContract]
public class WhiteListRequirement : Requirement
{
    /// <summary>
    /// The whitelisted parties for each accreditation role
    /// Comma separated list in app config
    /// </summary>
    [DataMember(Name = "allowedParties")]
    [Required]
    public List<KeyValuePair<AccreditationPartyTypes, string>> AllowedParties { get; set; }

    /// <summary>
    /// Default constructor, sets AllowedParties to new List
    /// </summary>
    public WhiteListRequirement()
    {
        AllowedParties = new List<KeyValuePair<AccreditationPartyTypes, string>>();
    }
}