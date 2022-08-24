using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Expired Consent Exception
/// FIXME! This is not used anywhere
/// </summary>
public class ExpiredConsentException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The consent for the accreditation has expired. Any evidence codes requiring consent for this accreditation is no longer available";

    /// <summary>
    /// Expired Consent Exception
    /// </summary>
    public ExpiredConsentException() : base(ErrorCode.ExpiredConsentException)
    {
    }

    /// <summary>
    /// Expired Consent Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public ExpiredConsentException(string? message) : base(ErrorCode.ExpiredConsentException, message)
    {
    }

    /// <summary>
    /// Expired Consent Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ExpiredConsentException(string? message, Exception? innerException) : base(ErrorCode.ExpiredConsentException, message, innerException)
    {
    }
}