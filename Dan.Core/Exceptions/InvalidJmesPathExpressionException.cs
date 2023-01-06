using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid JMES Path Exception
/// </summary>
public class InvalidJmesPathExpressionException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "Failure applying JMESPath transform";

    /// <summary>
    /// Invalid JMES Path Exception
    /// </summary>
    public InvalidJmesPathExpressionException() : base(ErrorCode.InvalidJmesPathExpressionException)
    {
    }

    /// <summary>
    /// Invalid JMES Path Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidJmesPathExpressionException(string? message) : base(ErrorCode.InvalidJmesPathExpressionException, message)
    {
    }

    /// <summary>
    /// Invalid JMES Path Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidJmesPathExpressionException(string? message, Exception? innerException) : base(ErrorCode.InvalidJmesPathExpressionException, message, innerException)
    {
    }
}