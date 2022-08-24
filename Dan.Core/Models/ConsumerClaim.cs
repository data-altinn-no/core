using System.Runtime.Serialization;

namespace Dan.Core.Models;

[DataContract]
public class ConsumerClaim
{
    /// <summary>
    /// Gets or sets the format of the identifier. Must always be "iso6523-actorid-upis"
    /// </summary>
    [DataMember(Name = "authority")]
    public string Authority { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier for the consumer. Must have ISO6523 prefix, which should be "0192:" for norwegian organization numbers
    /// </summary>
    [DataMember]
    public string ID { get; set; } = string.Empty;
}
