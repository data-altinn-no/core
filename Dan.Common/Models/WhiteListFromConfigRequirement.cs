namespace Dan.Common.Models;

/// <summary>
/// Whitelist from config requirement
/// </summary>
[DataContract]
public class WhiteListFromConfigRequirement : Requirement
{
    /// <summary>
    /// Owner requirement configuration
    /// </summary>
    [DataMember(Name = "ownerConfigKey")]
    [Required]
    public string? OwnerConfigKey { get; set; }

    /// <summary>
    /// Config for valid requestors
    /// </summary>
    [DataMember(Name = "requestorConfigKey")]
    [Required]
    public string? RequestorConfigKey { get; set; }

    /// <summary>
    /// Config for valid subjects
    /// </summary>
    [DataMember(Name = "subjectConfigKey")]
    [Required]
    public string? SubjectConfigKey { get; set; }
}