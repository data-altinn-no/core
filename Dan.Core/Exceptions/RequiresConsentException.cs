using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Requires Consent Exception
/// </summary>
public class RequiresConsentException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error regarding consent with one or more of the evidence codes requested";

    /// <summary>
    /// Requires Consent Exception
    /// </summary>
    public RequiresConsentException() : base(ErrorCode.RequiresConsentException)
    {
    }

    /// <summary>
    /// Requires Consent Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public RequiresConsentException(string? message) : base(ErrorCode.RequiresConsentException, message)
    {
    }

    /// <summary>
    /// Requires Consent Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public RequiresConsentException(string? message, Exception? innerException) : base(ErrorCode.RequiresConsentException, message, innerException)
    {
    }
}