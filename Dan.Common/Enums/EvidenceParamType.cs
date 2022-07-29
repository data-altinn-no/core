namespace Dan.Common.Enums;

/// <summary>
/// The type of the evidence parameter
/// </summary>
public enum EvidenceParamType
{
    /// <summary>
    /// Boolean (yes/no) parameter
    /// </summary>
    [EnumMember(Value = "boolean")]
    Boolean = 1,

    /// <summary>
    /// Any number, such as 45 or -234.53
    /// </summary>
    [EnumMember(Value = "number")]
    Number = 2,

    /// <summary>
    /// Any UTF-8 encoded string
    /// </summary>
    [EnumMember(Value = "string")]
    String = 3,

    /// <summary>
    /// ISO8601 Date and time
    /// </summary>
    [EnumMember(Value = "dateTime")]
    DateTime = 4,

    /// <summary>
    /// Binary attachment
    /// </summary>
    [EnumMember(Value = "attachment")]
    Attachment = 5,

    /// <summary>
    /// URI value
    /// </summary>
    [EnumMember(Value = "uri")]
    Uri = 6,
}