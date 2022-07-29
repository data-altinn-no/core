using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Authorization Failed Exception
/// </summary>
public class AuthorizationFailedException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The request has been denied authorization";

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    public AuthorizationFailedException() : base(ErrorCode.AuthorizationFailedException)
    {
    }

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public AuthorizationFailedException(string message) : base(ErrorCode.AuthorizationFailedException, message)
    {
    }

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public AuthorizationFailedException(string message, Exception innerException) : base(ErrorCode.AuthorizationFailedException, message, innerException)
    {
    }
}