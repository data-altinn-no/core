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
    /// Describes the regex in clear text
    /// </summary>
    [DataMember(Name = "subjectRegexDescription")]
    [Required]
    public string SubjectRegexDescription { get; set; } = "Any string";
}