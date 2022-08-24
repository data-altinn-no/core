using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Subject Exception
/// </summary>
public class InvalidOwnerException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the supplied owner";

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    public InvalidOwnerException() : base(ErrorCode.InvalidOwnerException)
    {
    }

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidOwnerException(string? message) : base(ErrorCode.InvalidOwnerException, message)
    {
    }

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidOwnerException(string? message, Exception? innerException) : base(ErrorCode.InvalidOwnerException, message, innerException)
    {
    }
}