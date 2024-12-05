using Newtonsoft.Json;

namespace Dan.PluginTest.Models;

[Serializable]
public class DatasetResponse
{
    [JsonProperty("test")]
    public string? Test { get; set; }
}