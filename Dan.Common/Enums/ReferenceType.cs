namespace Dan.Common.Enums;

/// <summary>
/// List of available types of references that can be included in a request
/// </summary>
public enum ReferenceType
{
    /// <summary>
    /// Reference used to link required information to a consent request
    /// </summary>
    [EnumMember(Value = "ConsentReference")]
    ConsentReference = 0,

    /// <summary>
    /// Arbitrary reference that can be set by a requestor
    /// </summary>
    [EnumMember(Value = "ExternalReference")]
    ExternalReference = 1
}