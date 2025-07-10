namespace Dan.Common.Exceptions;

/// <summary>
/// Exceptions when fetching data from CCR
/// </summary>
public class CcrException : DanException
{
    /// <summary>
    /// CCR Exception without any detailed message
    /// </summary>
    /// <param name="errorCode">DAN error code</param>
    public CcrException(ErrorCode errorCode) : base(errorCode)
    {
    }

    /// <summary>
    /// CCR Exception with detailed message
    /// </summary>
    /// <param name="errorCode">DAN error code</param>
    /// <param name="message">Error message</param>
    public CcrException(ErrorCode errorCode, string? message) : base(errorCode, message)
    {
    }

    /// <summary>
    /// CCR Exception with detailed message and inner exception
    /// </summary>
    /// <param name="errorCode">DAN error code</param>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public CcrException(ErrorCode errorCode, string? message, Exception? innerException) : base(errorCode, message, innerException)
    {
    }
}