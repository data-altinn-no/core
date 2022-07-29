namespace Dan.Common.Enums;

/// <summary>
/// The three parties to an accreditation
/// </summary>
public enum AccreditationPartyTypes
{
    /// <summary>
    /// The party the data is for 
    /// </summary>
    [EnumMember(Value = "Subject")]
    Subject = 1,

    /// <summary>
    /// The party requesting the data
    /// </summary>
    [EnumMember(Value = "Requestor")]
    Requestor = 2,

    /// <summary>
    /// The authenticated party 
    /// </summary>
    [EnumMember(Value = "Owner")]
    Owner = 3
}