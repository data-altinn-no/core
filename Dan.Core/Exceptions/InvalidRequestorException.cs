using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Requestor Exception
/// </summary>
public class InvalidRequestorException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the supplied requestor organization";

    /// <summary>
    /// Invalid Requestor Exception
    /// </summary>
    public InvalidRequestorException() : base(ErrorCode.InvalidRequestorException)
    {
    }

    /// <summary>
    /// Invalid Requestor Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidRequestorException(string message) : base(ErrorCode.InvalidRequestorException, message)
    {
    }

    /// <summary>
    /// Invalid Requestor Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidRequestorException(string message, Exception innerException) : base(ErrorCode.InvalidRequestorException, message, innerException)
    {
    }
}