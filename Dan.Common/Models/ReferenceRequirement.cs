namespace Dan.Common.Models;

[DataContract]
public class ReferenceRequirement : Requirement
{
    /// <summary>
    /// The type of the reference. Allowed references are defined by <see cref="Enums.ReferenceType"/>
    /// </summary>
    [DataMember(Name = "ReferenceType")]
    [Required]
    public ReferenceType ReferenceType { get; set; }

    /// <summary>
    /// A regular expression describing the accepted format of a reference. This field is ignored if the
    /// reference type is <see cref="ReferenceType.ConsentReference"/>
    /// </summary>
    [DataMember(Name = "AcceptedFormat")]
    public string AcceptedFormat { get; set; }
}