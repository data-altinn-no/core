using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Deleted Consent Exception
/// FIXME! This is not used anywhere
/// </summary>
public class DeletedConsentException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The consent that was previously granted has been revoked by a representative for the subject entity";

    /// <summary>
    /// Deleted Consent Exception
    /// </summary>
    public DeletedConsentException() : base(ErrorCode.DeletedConsentException)
    {
    }

    /// <summary>
    /// Deleted Consent Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public DeletedConsentException(string message) : base(ErrorCode.DeletedConsentException, message)
    {
    }

    /// <summary>
    /// Deleted Consent Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public DeletedConsentException(string message, Exception innerException) : base(ErrorCode.DeletedConsentException, message, innerException)
    {
    }
}