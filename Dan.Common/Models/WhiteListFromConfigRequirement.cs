namespace Dan.Common.Models;

/// <summary>
/// Whitelist from config requirement
/// </summary>
[DataContract]
public class WhiteListFromConfigRequirement : Requirement
{
    [DataMember(Name = "ownerConfigKey")]
    [Required]
    public string? OwnerConfigKey { get; set; }

    [DataMember(Name = "requestorConfigKey")]
    [Required]
    public string? RequestorConfigKey { get; set; }

    [DataMember(Name = "subjectConfigKey")]
    [Required]
    public string? SubjectConfigKey { get; set; }
}