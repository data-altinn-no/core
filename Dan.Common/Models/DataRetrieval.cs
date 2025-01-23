namespace Dan.Common.Models;

/// <summary>
/// For determining what dataset had data retrieved from and when
/// </summary>
[DataContract]
public class DataRetrieval
{
    /// <summary>
    /// Name of dataset that data was retrieved from
    /// </summary>
    [DataMember(Name = "evidenceCodeName")]
    public string? EvidenceCodeName { get; set; }

    /// <summary>
    /// When data was retrieved from dataset
    /// </summary>
    [DataMember(Name = "timeStamp")]
    public DateTime TimeStamp { get; set; }
}