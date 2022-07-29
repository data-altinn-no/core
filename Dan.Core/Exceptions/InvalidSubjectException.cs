using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Subject Exception
/// </summary>
public class InvalidSubjectException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the supplied subject organization";

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    public InvalidSubjectException() : base(ErrorCode.InvalidSubjectException)
    {
    }

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidSubjectException(string message) : base(ErrorCode.InvalidSubjectException, message)
    {
    }

    /// <summary>
    /// Invalid Subject Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidSubjectException(string message, Exception innerException) : base(ErrorCode.InvalidSubjectException, message, innerException)
    {
    }
}