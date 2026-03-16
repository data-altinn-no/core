using Newtonsoft.Json;

namespace Dan.Core.Models
{
    /// <summary>
    /// Represents an Altinn 3 consent request
    /// </summary>
    public class Altinn3ConsentRequest
    {
        [JsonProperty("id", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? Id { get; set; }

        [JsonProperty("from", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string From { get; set; } = string.Empty;

        [JsonProperty("requiredDelegator", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? RequiredDelegator { get; set; }

        [JsonProperty("to", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string To { get; set; } = string.Empty;

        [JsonProperty("validTo", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime ValidTo { get; set; }

        [JsonProperty("consentRights", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ConsentRight> ConsentRights { get; set; } = [];

        [JsonProperty("requestMessage", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ConsentRequestMessage? RequestMessage { get; set; }

        [JsonProperty("redirectUrl", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? RedirectUrl { get; set; }

        [JsonProperty("portalViewMode", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? PortalViewMode { get; set; }
    }
}
