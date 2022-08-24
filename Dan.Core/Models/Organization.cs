using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class Organization
{
    /// <summary>
    /// Gets or sets the name of the reportee.
    /// </summary>
    [DataMember]
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the type of reportee.
    /// </summary>
    [DataMember]
    public string? Type { get; set; }

    /// <summary>
    /// Gets or sets the organization number of the reportee. This is used only if the reportee type is an organization.
    /// </summary>
    [DataMember(EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? OrganizationNumber { get; set; }
}