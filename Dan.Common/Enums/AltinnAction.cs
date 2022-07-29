namespace Dan.Common.Enums;

/// <summary>
/// Types of actions in Altinn rights
/// </summary>
public enum AltinnAction
{
    /// <summary>
    /// Read from message box
    /// </summary>
    [EnumMember(Value = "Read")]
    Read = 1,

    /// <summary>
    /// Change forms and message
    /// </summary>
    [EnumMember(Value = "Write")]
    Write = 2,

    /// <summary>
    /// Sign forms 
    /// </summary>
    [EnumMember(Value = "Sign")]
    Sign = 3,

    /// <summary>
    /// Read archived elements
    /// </summary>
    [EnumMember(Value = "ArchiveRead")]
    ArchiveRead = 4,

    /// <summary>
    /// Delete archived elements
    /// </summary>
    [EnumMember(Value = "ArchiveDelete")]
    ArchiveDelete = 5,

    /// <summary>
    /// Generic access operation
    /// </summary>
    [EnumMember(Value = "Access")]
    Access = 8,
}