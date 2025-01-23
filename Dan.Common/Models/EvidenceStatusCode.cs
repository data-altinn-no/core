namespace Dan.Common.Models;

/// <summary>
/// Describing the availability status of the given evidence code used in the context of a Accreditation
/// </summary>
[DataContract]
public class EvidenceStatusCode : IEquatable<EvidenceStatusCode>
{
    /// <summary>
    /// Default unset value
    /// </summary>
    public static EvidenceStatusCode Unknown => new() { Code = (int)StatusCodeId.Unknown, Description = string.Empty };

    /// <summary>
    /// The requested information can be harvested at any time
    /// </summary>
    public static EvidenceStatusCode Available => new() { Code = (int)StatusCodeId.Available, Description = "The information is available for harvest" };

    /// <summary>
    /// The requested information is not available pending a consent request in Altinn
    /// </summary>
    public static EvidenceStatusCode PendingConsent => new() { Code = (int)StatusCodeId.PendingConsent, Description = "Awaiting consent from subject entity representative" };

    /// <summary>
    /// The requested information is not available due to a consent request being denied or an existing consent has been revoked
    /// </summary>
    public static EvidenceStatusCode Denied => new() { Code = (int)StatusCodeId.Denied, Description = "Consent request denied" };

    /// <summary>
    /// The requested information is not available due to a consent delegation being expired
    /// </summary>
    public static EvidenceStatusCode Expired => new() { Code = (int)StatusCodeId.Expired, Description = "Consent expired" };

    /// <summary>
    /// The requested information is not yet available from the asynchronous source
    /// </summary>
    public static EvidenceStatusCode Waiting => new() { Code = (int)StatusCodeId.Waiting, Description = "Awaiting data from source" };

    /// <summary>
    /// The requested information is not yet available from the asynchronous source
    /// </summary>
    public static EvidenceStatusCode AggregateUnknown => new() { Code = (int)StatusCodeId.AggregateUnknown, Description = "The aggredate evidence status is unknown due to one or more asynchronous evidence codes in the accreditation. See /evidence/{accreditationId} for asynchronous evidence code status" };

    /// <summary>
    /// The requested information is not yet available from the asynchronous source
    /// </summary>
    public static EvidenceStatusCode Unavailable => new() { Code = (int)StatusCodeId.Unavailable, Description = "The evidence code is no longer available and cannot be checked for status or harvested." };

    /// <summary>
    /// Status code
    /// </summary>
    [Required]
    [DataMember(Name = "code")]
    public int Code { get; init; } = (int)StatusCodeId.Unknown;

    /// <summary>
    /// Description of the status code
    /// </summary>
    [Required]
    [DataMember(Name = "description")]
    public string? Description { get; set; }

    /// <summary>
    /// For asynchronous data sources, might include a hint at which point another attempt should be made to see if the information requested is available
    /// </summary>
    [Required]
    [DataMember(Name = "retryAt")]
    [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
    public DateTime? RetryAt { get; set; }
    
    // Some default methods on method, shoud be self describing without descriptions what they do
#pragma warning disable 1591    
    public bool Equals(EvidenceStatusCode? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Code == other.Code;
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EvidenceStatusCode)obj);
    }
    
    public override int GetHashCode()
    {
        return Code.GetHashCode();
    }

    public static bool operator ==(EvidenceStatusCode? left, EvidenceStatusCode? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EvidenceStatusCode? left, EvidenceStatusCode? right)
    {
        return !Equals(left, right);
    }
#pragma warning restore 1591
}