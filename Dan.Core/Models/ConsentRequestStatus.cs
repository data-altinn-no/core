using System.ComponentModel;
using Dan.Core.Helpers;

namespace Dan.Core.Models;

/// <summary>
/// Enum for determining the status of an consent request 
/// </summary>
[TypeConverter(typeof(DefaultIgnoreCaseEnumConverter<ConsentRequestStatus>))]
public enum ConsentRequestStatus
{
    /// <summary>
    /// Should not be used as a status for an consent request.
    /// </summary>
    None = 0,

    /// <summary>
    /// Used when a consent request is unopened.
    /// </summary>
    Unopened = 1,

    /// <summary>
    /// Used when a consent request is opened.
    /// </summary>
    Opened = 2,

    /// <summary>
    /// Used when a consent request is accepted.
    /// </summary>
    Accepted = 3,

    /// <summary>
    /// Used when a consent request is rejected.
    /// </summary>
    Rejected = 4
}
