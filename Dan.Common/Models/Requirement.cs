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
    public string RequirementType;

    /// <summary>
    /// Action to take if requirement is not satisified
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember(Name = "failureAction")]
    public FailureAction FailureAction;

    /// <summary>
    ///  A requirement may apply to one, several or all servicecontexts using the dataset
    /// </summary>
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    [DataMember(Name = "appliesToServiceContext")]
    public List<string> AppliesToServiceContext;

}