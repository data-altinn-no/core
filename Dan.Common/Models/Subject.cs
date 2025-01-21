namespace Dan.Common.Models;

/// <summary>
/// Model to get subject from http request
/// </summary>
[DataContract]
public class Subject
{
    /// <summary>
    /// Subject identifier
    /// </summary>
    [DataMember(Name = "subject")]
    public string? SubjectId { get; set; }
}