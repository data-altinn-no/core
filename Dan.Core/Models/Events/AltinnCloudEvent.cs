using Newtonsoft.Json;

namespace Dan.Core.Models.Events;

/// <summary>
/// A CloudEvent (v1.0) as accepted by the Altinn Events API, including the Altinn extension attributes
/// <c>resource</c> and <c>resourceinstance</c>. See https://docs.altinn.studio/en/events/
/// </summary>
public class AltinnCloudEvent
{
    /// <summary>
    /// Unique event id. Also sent as Idempotency-Key.
    /// </summary>
    [JsonProperty("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// CloudEvents specification version, always "1.0"
    /// </summary>
    [JsonProperty("specversion")]
    public string SpecVersion { get; set; } = "1.0";

    /// <summary>
    /// The event type, e.g. no.digdir.dataaltinnno.consent.granted. Subscribers filter on this with exact match.
    /// </summary>
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// URI identifying the context in which the event happened
    /// </summary>
    [JsonProperty("source")]
    public string Source { get; set; } = string.Empty;

    /// <summary>
    /// The party the event concerns, e.g. urn:altinn:organization:identifier-no:{orgnr}. Subscribers filter on this.
    /// </summary>
    [JsonProperty("subject", NullValueHandling = NullValueHandling.Ignore)]
    public string? Subject { get; set; }

    /// <summary>
    /// The resource the event belongs to, urn:altinn:resource:{resourceId}. Must exist in the Altinn Resource Registry.
    /// </summary>
    [JsonProperty("resource")]
    public string Resource { get; set; } = string.Empty;

    /// <summary>
    /// The instance of the resource the event concerns, here the accreditation id
    /// </summary>
    [JsonProperty("resourceinstance", NullValueHandling = NullValueHandling.Ignore)]
    public string? ResourceInstance { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    [JsonProperty("time")]
    public DateTime Time { get; set; }

    /// <summary>
    /// Content type of the data attribute
    /// </summary>
    [JsonProperty("datacontenttype")]
    public string DataContentType { get; set; } = "application/json";

    /// <summary>
    /// Event payload
    /// </summary>
    [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
    public object? Data { get; set; }
}

/// <summary>
/// Payload of a consent event. Deliberately contains no personal identifiers; clients fetch the
/// accreditation from data.altinn.no to get the full state.
/// </summary>
public class ConsentEventData
{
    /// <summary>
    /// The accreditation id
    /// </summary>
    [JsonProperty("accreditationId")]
    public string AccreditationId { get; set; } = string.Empty;

    /// <summary>
    /// The consent outcome: granted, denied or expired
    /// </summary>
    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// The service context the accreditation belongs to
    /// </summary>
    [JsonProperty("serviceContext", NullValueHandling = NullValueHandling.Ignore)]
    public string? ServiceContext { get; set; }

    /// <summary>
    /// The consent reference supplied by the client in the authorization request
    /// </summary>
    [JsonProperty("consentReference", NullValueHandling = NullValueHandling.Ignore)]
    public string? ConsentReference { get; set; }

    /// <summary>
    /// The external reference supplied by the client in the authorization request
    /// </summary>
    [JsonProperty("externalReference", NullValueHandling = NullValueHandling.Ignore)]
    public string? ExternalReference { get; set; }

    /// <summary>
    /// How long the accreditation is valid
    /// </summary>
    [JsonProperty("validTo")]
    public DateTime ValidTo { get; set; }
}
