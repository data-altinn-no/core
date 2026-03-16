using Newtonsoft.Json;

namespace Dan.Core.Models
{
    /// <summary>
    /// Represents a resource in an Altinn 3 consent request/response
    /// </summary>
    public class ConsentResource
    {
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("value", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Represents metadata for a consent right
    /// </summary>
    public class ConsentMetadata
    {
        [JsonProperty("additionalProp1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp1 { get; set; }

        [JsonProperty("additionalProp2", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp2 { get; set; }

        [JsonProperty("additionalProp3", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp3 { get; set; }
    }

    /// <summary>
    /// Represents a consent right with actions and resources
    /// </summary>
    public class ConsentRight
    {
        [JsonProperty("action", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<string> Action { get; set; } = [];

        [JsonProperty("resource", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public List<ConsentResource> Resource { get; set; } = [];

        [JsonProperty("metadata", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ConsentMetadata? Metadata { get; set; }
    }

    /// <summary>
    /// Represents a request message with additional properties
    /// </summary>
    public class ConsentRequestMessage
    {
        [JsonProperty("additionalProp1", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp1 { get; set; }

        [JsonProperty("additionalProp2", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp2 { get; set; }

        [JsonProperty("additionalProp3", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string? AdditionalProp3 { get; set; }
    }

    /// <summary>
    /// Represents a consent request event
    /// </summary>
    public class ConsentRequestEvent
    {
        [JsonProperty("consentEventID")]
        public string ConsentEventId { get; set; } = string.Empty;

        [JsonProperty("created")]
        public DateTime Created { get; set; }

        [JsonProperty("performedBy")]
        public string PerformedBy { get; set; } = string.Empty;

        [JsonProperty("eventType")]
        public string EventType { get; set; } = string.Empty;

        [JsonProperty("consentRequestID")]
        public string ConsentRequestId { get; set; } = string.Empty;
    }
}

