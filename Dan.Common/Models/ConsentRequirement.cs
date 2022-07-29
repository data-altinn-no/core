namespace Dan.Common.Models;

/// <summary>
/// Consent requiremnent
/// </summary>
[DataContract]
public class ConsentRequirement : Requirement
{
    /// <summary>
    /// Service code for consent service in Altinn
    /// </summary>
    [DataMember(Name = "serviceCode")]
    [Required]
    public string ServiceCode { get; set; }

    /// <summary>
    /// Service edition for consent service in Altinn
    /// </summary>
    [DataMember(Name = "serviceEdition")]
    [Required]
    public int ServiceEdition { get; set; }

    /// <summary>
    /// Indicate whether the service requires SRR approval from the owner
    /// </summary>
    [DataMember(Name = "requiresSrr")]
    [Required]
    public bool RequiresSrr { get; set; }

    /// <summary>
    /// How long the consent needs to be valid. If not supplied, use validto on accreditation
    /// </summary>
    [DataMember(Name = "consentPeriodInDays")]
    [Required]
    public int? ConsentPeriodInDays { get; set; }
}