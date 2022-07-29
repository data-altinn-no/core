namespace Dan.Common.Models;

[DataContract]
public class Subject
{
    [DataMember(Name = "subject")]
    public string SubjectId { get; set; }
}