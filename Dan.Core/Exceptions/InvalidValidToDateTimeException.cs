using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid valid to date Exception
/// </summary>
public class InvalidValidToDateTimeException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the supplied valid to date";

    /// <summary>
    /// Invalid valid to date Exception
    /// </summary>
    public InvalidValidToDateTimeException() : base(ErrorCode.InvalidValidToDateTimeException)
    {
    }

    /// <summary>
    /// Invalid valid to date Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidValidToDateTimeException(string? message) : base(ErrorCode.InvalidValidToDateTimeException, message)
    {
    }

    /// <summary>
    /// Invalid valid to date Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidValidToDateTimeException(string? message, Exception? innerException) : base(ErrorCode.InvalidValidToDateTimeException, message, innerException)
    {
    }
}