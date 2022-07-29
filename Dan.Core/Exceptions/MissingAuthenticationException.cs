using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

public class MissingAuthenticationException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the provided authentication credentials";

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    public MissingAuthenticationException() : base(ErrorCode.MissingAuthenticationException)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public MissingAuthenticationException(string message) : base(ErrorCode.MissingAuthenticationException, message)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public MissingAuthenticationException(string message, Exception innerException) : base(ErrorCode.MissingAuthenticationException, message, innerException)
    {
    }
}