namespace Dan.Common.Exceptions;

/// <summary>
/// Base exception for errors thrown in evidence source implementations
/// </summary>
public abstract class EvidenceSourceException : DanException
{
    /// <summary>
    /// Evidence Source Exception
    /// </summary>
    /// <param name="errorCode">Error Code</param>
    protected EvidenceSourceException(ErrorCode errorCode) : base(errorCode)
    {
    }

    /// <summary>
    /// Evidence Source Exception
    /// </summary>
    /// <param name="errorCode">Error Code</param>
    /// <param name="detailErrorCode">Evidence Source specific error code</param>
    protected EvidenceSourceException(ErrorCode errorCode, int detailErrorCode) : base(errorCode)
    {
        DetailErrorCode = detailErrorCode;
    }

    /// <summary>
    /// Evidence Source Exception
    /// </summary>
    /// <param name="errorCode">Error Code</param>
    /// <param name="detailErrorCode">Evidence Source specific error code</param>
    /// <param name="message">The message</param>
    protected EvidenceSourceException(ErrorCode errorCode, int detailErrorCode, string? message) : base(errorCode, message)
    {
        DetailErrorCode = detailErrorCode;
    }

    /// <summary>
    /// Evidence Source Exception
    /// </summary>
    /// <param name="errorCode">Error Code</param>
    /// <param name="detailErrorCode">Evidence Source specific error code</param>
    /// <param name="message">The message</param>
    /// <param name="innerException">Inner Exception</param>
    protected EvidenceSourceException(ErrorCode errorCode, int detailErrorCode, string? message, Exception? innerException) : base(errorCode, message, innerException)
    {
        DetailErrorCode = detailErrorCode;
    }
}