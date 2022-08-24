using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Organization Exception
/// </summary>
public class InvalidAuthorizationRequestException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The authorization request contained one or more errors";

    /// <summary>
    /// Invalid Authorization Request Exception
    /// </summary>
    public InvalidAuthorizationRequestException() : base(ErrorCode.InvalidAuthorizationRequestException)
    {
    }

    /// <summary>
    /// Invalid Authorization Request Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidAuthorizationRequestException(string? message) : base(ErrorCode.InvalidAuthorizationRequestException, message)
    {
    }

    /// <summary>
    /// Invalid Authorization Request Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidAuthorizationRequestException(string? message, Exception? innerException) : base(ErrorCode.InvalidAuthorizationRequestException, message, innerException)
    {
    }
}