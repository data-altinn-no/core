using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;
public class InvalidAccessTokenException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the provided access token";

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    public InvalidAccessTokenException() : base(ErrorCode.InvalidAccessTokenException)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidAccessTokenException(string message) : base(ErrorCode.InvalidAccessTokenException, message)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidAccessTokenException(string message, Exception innerException) : base(ErrorCode.InvalidAccessTokenException, message, innerException)
    {
    }

}