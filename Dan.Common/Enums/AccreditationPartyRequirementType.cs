namespace Dan.Common.Enums;

/// <summary>
/// List of relational requirements for subjects, owners and requestors
/// </summary>
public enum AccreditationPartyRequirementType
{
    /// <summary>
    /// Requestor must also be owner
    /// </summary>
    [EnumMember(Value = "RequestorAndOwnerAreEqual")]
    RequestorAndOwnerAreEqual = 0,

    /// <summary>
    /// Subject must also be owner
    /// </summary>
    [EnumMember(Value = "SubjectAndOwnerAreEqual")]
    SubjectAndOwnerAreEqual = 1,

    /// <summary>
    /// Requestor must also be subject
    /// </summary>
    [EnumMember(Value = "RequestorAndSubjectAreEqual")]
    RequestorAndSubjectAreEqual = 2,

    /// <summary>
    /// Requestor cannot also be owner
    /// </summary>
    [EnumMember(Value = "RequestorAndOwnerAreNotEqual")]
    RequestorAndOwnerAreNotEqual = 3,

    /// <summary>
    /// Requestor cannot also be subject
    /// </summary>
    [EnumMember(Value = "RequestorAndSubjectAreNotEqual")]
    RequestorAndSubjectAreNotEqual = 4,
}