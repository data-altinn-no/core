namespace Dan.Common.Models;

/// <summary>
/// Requirement for subjects that differ from organisation number or Norwegian SSN
/// </summary>
public class CustomSubjectRequirement : Requirement
{
    /// <summary>
    /// Regex used to validate custom subject. Defaults to \w+ for any letters and digits
    /// </summary>
    [DataMember(Name = "subjectRegex")]
    [Required]
    public string SubjectRegex { get; set; } = @"\w+";

    /// <summary>
    /// Flag to set if should skip for checking if subject is SSN or Org number. Default to false
    /// </summary>
    [DataMember(Name = "skipRegularSubjectValidation")]
    [Required]
    public bool SkipRegularSubjectValidation { get; set; }
}