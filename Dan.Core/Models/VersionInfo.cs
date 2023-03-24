using Newtonsoft.Json;

namespace Dan.Core.Models;

public class VersionInfo
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonProperty("built")]
    public string Built { get; set; } = string.Empty;

    [JsonProperty("commit")]
    public string Commit { get; set; } = string.Empty;

    [JsonProperty("commitDate")]
    public string CommitDate { get; set; } = string.Empty;
}
