namespace Dan.Common.Models;

/// <summary>
/// Legalbasis requirement
/// </summary>
[DataContract]
public class LegalBasisRequirement : Requirement
{
    /// <summary>
    /// Define which legal basis types that are acceptable
    /// </summary>
    [DataMember(Name = "validLegalBasisTypes")]
    public LegalBasisType ValidLegalBasisTypes { get; set; }
}