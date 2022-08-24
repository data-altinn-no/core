using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Asynchronous Evidence Still Waiting Exception
/// </summary>
public class AsyncEvidenceStillWaitingException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The data for the requested evidence is not yet available";

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    public AsyncEvidenceStillWaitingException() : base(ErrorCode.AsyncEvidenceStillWaitingException)
    {
    }

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public AsyncEvidenceStillWaitingException(string? message) : base(ErrorCode.AsyncEvidenceStillWaitingException, message)
    {
    }

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public AsyncEvidenceStillWaitingException(string? message, Exception? innerException) : base(ErrorCode.AsyncEvidenceStillWaitingException, message, innerException)
    {
    }
}