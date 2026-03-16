using Newtonsoft.Json;

namespace Dan.Core.Models
{
    /// <summary>
    /// Represents an error response from Altinn 3 consent API
    /// </summary>
    public class Altinn3ConsentRequestError
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("title")]
        public string Title { get; set; } = string.Empty;

        [JsonProperty("status")]
        public int Status { get; set; }

        [JsonProperty("detail")]
        public string Detail { get; set; } = string.Empty;

        [JsonProperty("instance")]
        public string Instance { get; set; } = string.Empty;
    }
}
