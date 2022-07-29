namespace Dan.Common.Enums;

/// <summary>
/// Evidence status code ids
/// </summary>
public enum StatusCodeId
{
    /// <summary>
    /// The evidence is available for harvesting
    /// </summary>
    Available = 1,

    /// <summary>
    /// Pending consent
    /// </summary>
    PendingConsent = 2,

    /// <summary>
    /// Access to the evidence was denied
    /// </summary>
    Denied = 3,

    /// <summary>
    /// Access to the evidence has expired
    /// </summary>
    Expired = 4,

    /// <summary>
    /// Access to the evidence value is still pending (for asynchronous data sources)
    /// </summary>
    Waiting = 5,

    /// <summary>
    /// Only used in list views of accreditations, and used when the aggregate evidence status is unknown due to one or more asynchronous evidence codes in the accreditation that may be expensive to look up 
    /// </summary>
    AggregateUnknown = 6,

    /// <summary>
    /// Used if an accreditation refers to a evidence code that is not longer available (ie. removed from the ES)
    /// </summary>
    Unavailable = 7,
}