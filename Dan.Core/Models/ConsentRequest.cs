using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class ConsentRequest
{
    /// <summary>
    ///  Gets or sets The AuthorizationCode of a valid ConsentRequest
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Guid AuthorizationCode { get; set; }

    /// <summary>
    /// Gets or sets the status of an AuthorizationRequest
    /// </summary>
    [DataMember]
    [JsonConverter(typeof(StringEnumConverter))]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public ConsentRequestStatus RequestStatus { get; set; }

    /// <summary>
    ///  Gets or sets The OrgID/personalID for who get consent 
    /// </summary>
    [DataMember(IsRequired = true)]
    public string CoveredBy { get; set; }

    /// <summary>
    ///  Gets or sets The personalID who offer the consent
    /// </summary>
    [DataMember(IsRequired = true)]
    public string OfferedBy { get; set; }

    /// <summary>
    ///  Gets or sets The name who offer the consent
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string OfferedByName { get; set; }

    /// <summary>
    ///  Gets or sets The PersonalID/Org, ThirdPart who can use the consent 
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string HandledBy { get; set; }

    /// <summary>
    ///  Gets or sets The PersonaID for particular person who can use this consent 
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string RequiredDelegator { get; set; }

    /// <summary>
    ///  Gets or sets The Name for particular person who can use this consent 
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string RequiredDelegatorName { get; set; }

    /// <summary>
    ///  Gets or sets The ValidTo is the time that consent can be valid
    /// </summary>
    [DataMember]
    public DateTime ValidTo { get; set; }

    /// <summary>
    ///  Gets or sets The RedirectUrl is a link that sends the user back to the external website after he/she made an operation in Altinn
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string RedirectUrl { get; set; }

    /// <summary>
    ///  Gets or sets The RequestServices are all information of services which consent need 
    /// </summary>
    [DataMember(IsRequired = true)]
    public List<ConsentRequestResource> RequestResources { get; set; }

    /// <summary>
    ///  Gets or sets The RequestMessage which consent need
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, string> RequestMessage { get; set; }

    /// <summary>
    ///  Gets or sets the ErrorObject if any if the model is not valid
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<ConsentRequestError> Errors { get; set; }
}
