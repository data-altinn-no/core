namespace Dan.Common.Models;

/// <summary>
/// Maskinporten requirement
/// </summary>
[DataContract]
public class MaskinportenScopeRequirement : Requirement
{
    /// <summary>
    /// The scopes required to from the requestor access the evidence source
    /// </summary>
    [DataMember(Name = "requiredScopes")]
    [Required]
    public List<string> RequiredScopes { get; set; }

    public MaskinportenScopeRequirement()
    {
        RequiredScopes = new List<string>();
    }
}