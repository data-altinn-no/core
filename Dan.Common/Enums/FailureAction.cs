namespace Dan.Common.Enums;

public enum FailureAction
{
    /// <summary>
    /// Will deny the entire authorization request if requirement is not satisifed. This is the default behaviour
    /// </summary>
    [EnumMember(Value = "deny")]
    Deny = 0,

    /// <summary>
    /// Will cause the evidence code to be removed from the list of evidence codes in the accreditation, but will not fail the request. 
    /// </summary>
    [EnumMember(Value = "skip")]
    Skip = 1
}