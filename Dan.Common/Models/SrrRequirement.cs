namespace Dan.Common.Models;

/// <summary>
/// SRR requirement
/// </summary>
[DataContract]
public class SrrRequirement : Requirement
{
    /// <summary>
    /// The service code for the Altinn service
    /// </summary>
    [DataMember(Name = "serviceCode")]
    [Required]
    public string ServiceCode { get; set; }

    /// <summary>
    /// The service edition for the Altinn service
    /// </summary>
    [DataMember(Name = "serviceEdition")]
    [Required]
    public string ServiceEdition { get; set; }
}