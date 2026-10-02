using System.Security.Cryptography;
using System.Text;

namespace Dan.Common.Models;

/// <summary>
/// Outbox record for a consent event that should be (or has been) published to Altinn Events.
/// Stored on the accreditation so publishing is at-least-once: the record is persisted before the
/// first publish attempt, and a timer retries unpublished records.
/// </summary>
[DataContract]
public class ConsentEventRecord
{
    /// <summary>
    /// The CloudEvent id, also used as Idempotency-Key towards Altinn Events. Deterministic per accreditation and event type,
    /// so a duplicate publish attempt is de-duplicated by Altinn.
    /// </summary>
    [DataMember(Name = "eventId")]
    public string EventId { get; set; } = string.Empty;

    /// <summary>
    /// The CloudEvent type, see <see cref="ConsentEventTypes"/>
    /// </summary>
    [DataMember(Name = "type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// When the event occurred (UTC). Used as the CloudEvent time attribute.
    /// </summary>
    [DataMember(Name = "created")]
    public DateTime Created { get; set; }

    /// <summary>
    /// Whether the event has been accepted by Altinn Events
    /// </summary>
    [DataMember(Name = "published")]
    public bool Published { get; set; }

    /// <summary>
    /// When the event was accepted by Altinn Events (UTC)
    /// </summary>
    [DataMember(Name = "publishedAt")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? PublishedAt { get; set; }

    /// <summary>
    /// Number of publish attempts made
    /// </summary>
    [DataMember(Name = "attempts")]
    public int Attempts { get; set; }

    /// <summary>
    /// When the last publish attempt was made (UTC)
    /// </summary>
    [DataMember(Name = "lastAttempt")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? LastAttempt { get; set; }

    /// <summary>
    /// Error message from the last failed publish attempt, cleared on success
    /// </summary>
    [DataMember(Name = "lastError")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public string? LastError { get; set; }

    /// <summary>
    /// Creates a deterministic event id (GUID format) from the accreditation id and event type.
    /// </summary>
    /// <param name="accreditationId">The accreditation id</param>
    /// <param name="type">The event type</param>
    /// <returns>A GUID string that is stable for the same input</returns>
    public static string CreateEventId(string accreditationId, string type)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{accreditationId}:{type}"));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);

        // Mark as a RFC 4122 name-based (version 5 style) GUID so it is clearly not random
        guidBytes[7] = (byte)((guidBytes[7] & 0x0F) | 0x50);
        guidBytes[8] = (byte)((guidBytes[8] & 0x3F) | 0x80);

        return new Guid(guidBytes).ToString();
    }
}
