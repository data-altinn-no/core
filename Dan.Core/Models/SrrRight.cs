using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class SrrRight
{
    /// <summary>
    /// Gets or sets the identifier for a the right.
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int? Id { get; set; }

    /// <summary>
    /// Gets or sets the service code of the service that this right gives access to.
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string ServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the service edition code of the service that this right gives access to.
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public int ServiceEditionCode { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the person or organization that this right gives access for.
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string Reportee { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the operation type that this right is giving to the reportee.
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string Right { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the date and time for when the right will expire.
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public DateTime ValidTo { get; set; }

    /// <summary>
    /// Gets or sets an object representing the conditions of this right. 
    /// </summary>
    [DataMember(IsRequired = true)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public SrrRightCondition Condition { get; set; } = new();

    /// <summary>
    /// Gets or sets the result of operations in SRR. This is used by add, update and delete operations in the API.
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? OperationStatus { get; set; }
}
