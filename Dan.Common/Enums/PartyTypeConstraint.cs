namespace Dan.Common.Enums;

/// <summary>
/// Various requirements to the three parties to an accreditation
/// </summary>
public enum PartyTypeConstraint
{
    /// <summary>
    /// Private person
    /// </summary>
    [EnumMember(Value = "PrivatePerson")]
    PrivatePerson = 0,

    /// <summary>
    /// Public agency
    /// </summary>
    [EnumMember(Value = "PublicAgency")]
    PublicAgency = 1,

    /// <summary>
    /// Private enterprise
    /// </summary>
    [EnumMember(Value = "PrivateEnterprise")]
    PrivateEnterprise = 2,

    /// <summary>
    /// Enterprises in a specific industry
    /// </summary>
    [EnumMember(Value = "NACECode")]
    // ReSharper disable once InconsistentNaming
    NACECode = 3,

    /// <summary>
    /// Enterprises in a specific sector
    /// </summary>
    [EnumMember(Value = "SectorCode")]
    SectorCode = 4,

    /// <summary>
    /// Invalid party
    /// </summary>
    [EnumMember(Value = "Invalid")]
    Invalid = 8,

    /// <summary>
    /// Foreign party
    /// </summary>
    [EnumMember(Value = "Foreign")]
    Foreign = 9
}