namespace Dan.Common.Models;

[DataContract]
public class DataRetrieval
{
    [DataMember(Name = "evidenceCodeName")]
    public string EvidenceCodeName { get; set; }

    [DataMember(Name = "timeStamp")]
    public DateTime TimeStamp { get; set; }
}