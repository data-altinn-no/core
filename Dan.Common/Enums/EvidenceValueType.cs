namespace Dan.Common.Enums;

/// <summary>
/// The type of the evidence value
/// </summary>
public enum EvidenceValueType
{
    /// <summary>
    /// Boolean (yes/no) values
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
    /// Binary attachment (Base64-encoded)
    /// </summary>
    [EnumMember(Value = "attachment")]
    Attachment = 5,

    /// <summary>
    /// URI value
    /// </summary>
    [EnumMember(Value = "uri")]
    Uri = 6,

    /// <summary>
    /// Currency value
    /// </summary>
    [EnumMember(Value = "amount")]
    Amount = 7,

    /// <summary>
    /// Arbitrary JSON
    /// </summary>
    [EnumMember(Value = "jsonSchema")]
    JsonSchema = 8,

    /// <summary>
    /// Raw binary (only available without envelope, cannot be combined with other values)
    /// </summary>
    [EnumMember(Value = "binary")]
    Binary = 9,
}