using Newtonsoft.Json;

namespace Dan.Core.Models
{
    /// <summary>
    /// Represents an Altinn 3 consent response
    /// </summary>
    public class Altinn3ConsentResponse
    {
        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("from")]
        public string? From { get; set; }

        [JsonProperty("to")]
        public string? To { get; set; }

        [JsonProperty("requiredDelegator")]
        public string? RequiredDelegator { get; set; }

        [JsonProperty("handledBy")]
        public string? HandledBy { get; set; }

        [JsonProperty("validTo")]
        public DateTime ValidTo { get; set; }

        [JsonProperty("consentRights")]
        public ConsentRight[] ConsentRights { get; set; } = [];

        [JsonProperty("requestMessage")]
        public ConsentRequestMessage? RequestMessage { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; } = string.Empty;

        [JsonProperty("consented")]
        public DateTime? Consented { get; set; }

        [JsonProperty("redirectUrl")]
        public string? RedirectUrl { get; set; }

        [JsonProperty("consentRequestEvents")]
        public ConsentRequestEvent[]? ConsentRequestEvents { get; set; }

        [JsonProperty("viewUri")]
        public string? ViewUri { get; set; }

        [JsonProperty("portalViewMode")]
        public string? PortalViewMode { get; set; }
    }
}
