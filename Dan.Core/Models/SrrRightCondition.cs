using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Dan.Core.Models;

public class SrrRightCondition
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SrrRightCondition"/> class from a "classic" SRR condition string.
    /// </summary>
    public SrrRightCondition(string conditionString)
    {
        AllowedRedirectDomain = new List<string>();

        var conditions = conditionString.Split(',');
        foreach (var c in conditions)
        {
            var idx = c.IndexOf(':');
            string field;
            if (idx == -1)
            {
                //KeepSessionAlive
                field = c.ToUpper();
            }
            else
            {
                field = c.Substring(0, idx).ToUpper();
            }

            switch (field)
            {
                case "HANDLEDBY":
                    HandledBy = c.Substring(idx + 1);
                    break;
                case "KEEPSESSIONALIVE":
                    KeepSessionAlive = true;
                    break;
                case "ALLOWEDREDIRECTDOMAIN":
                    var domains = c.Substring(idx + 1).Split(';');
                    foreach (var d in domains)
                    {
                        AllowedRedirectDomain.Add(d);
                    }
                    break;
            }

        }
    }

    /// <summary>
    /// Gets or sets the organization that is handing it
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public string? HandledBy { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether should keep session alive
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public bool? KeepSessionAlive { get; set; }

    /// <summary>
    /// Gets or sets the list containing the allowed redirect domain urls
    /// </summary>
    [DataMember(IsRequired = false, EmitDefaultValue = false)]
    [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore)]
    public List<string> AllowedRedirectDomain { get; set; }
}
