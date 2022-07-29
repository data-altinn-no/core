namespace Dan.Common.Enums;

/// <summary>
/// Consent status as derived from the accreditation or returned from the Altinn API
/// </summary>
public enum ConsentStatus
{
    /// <summary>
    /// Consent request has not yet been answered 
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Consent request has been granted
    /// </summary>
    Granted = 1,

    /// <summary>
    /// Consent request has been denied
    /// </summary>
    Denied = 2,

    /// <summary>
    /// Consent request has been previously granted, now denied
    /// </summary>
    Revoked = 3,

    /// <summary>
    /// Consent request has expired
    /// </summary>
    Expired = 4
}