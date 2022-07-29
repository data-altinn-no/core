using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class ConsentRequestResource
{
    /// <summary>
    /// Gets or sets the ServiceCode that is requested
    /// </summary>
    [DataMember(IsRequired = true)]
    public string ServiceCode { get; set; }

    /// <summary>
    ///  Gets or sets the ServiceEditionCode that is requested
    /// </summary>
    [DataMember(IsRequired = true)]
    public int ServiceEditionCode { get; set; }

    /// <summary>
    ///  Gets or sets any metadata for the service, if required
    /// </summary>
    [DataMember]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public Dictionary<string, string> Metadata { get; set; }
}
