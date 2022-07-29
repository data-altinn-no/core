using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Internal Server Error Exception
/// </summary>
public class InternalServerErrorException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an internal server error processing your request.";

    /// <summary>
    /// Internal Server Error Exception
    /// </summary>
    public InternalServerErrorException() : base(ErrorCode.InternalServerErrorException)
    {
    }

    /// <summary>
    /// Internal Server Error Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InternalServerErrorException(string message) : base(ErrorCode.InternalServerErrorException, message)
    {
    }

    /// <summary>
    /// Internal Server Error Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InternalServerErrorException(string message, Exception innerException) : base(ErrorCode.InternalServerErrorException, message, innerException)
    {
    }
}