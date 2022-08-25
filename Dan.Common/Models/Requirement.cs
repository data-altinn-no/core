namespace Dan.Common.Models;

/// <summary>
/// Base class for all authorization requirements
/// </summary>
[DataContract]
public class Requirement
{
    /// <summary>
    ///  Used for serializing only
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember(Name = "type")]
    public string? RequirementType;

    /// <summary>
    /// Action to take if requirement is not satisified
    /// </summary>
    [DataMember(Name = "failureAction")]
    public FailureAction FailureAction;

    /// <summary>
    ///  A requirement may apply to one, several or all servicecontexts using the dataset
    /// </summary>
    [DataMember(Name = "appliesToServiceContext")]
    public List<string> AppliesToServiceContext = new();

    [DataMember(Name = "requiredOnEvidenceHarvester")]
    public bool RequiredOnEvidenceHarvester = true;

    public bool ShouldSerializeAppliesToServiceContext()
    {
        // This causes Json.NET to skip serializing AppliesToServiceContext if it's empty, as it
        // may be confusing that this being empty means it applies to _all_ service contexts
        return AppliesToServiceContext.Count > 0;
    }
}