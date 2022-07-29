namespace Dan.Common.Exceptions;

/// <summary>
/// Transient evidence source exception. This is used when the evidence source is not currently able to respond due to overloading or temporary downtime. 
/// </summary>
public class EvidenceSourceTransientException : EvidenceSourceException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was a temporary error with one of the evidence codes requested";

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    public EvidenceSourceTransientException() : base(ErrorCode.EvidenceSourceTransientException)
    {
    }

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    public EvidenceSourceTransientException(int detailErrorCode) : base(ErrorCode.EvidenceSourceTransientException, detailErrorCode)
    {
    }

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    /// <param name="message">
    /// Error Message
    /// </param>
    public EvidenceSourceTransientException(int detailErrorCode, string message) : base(ErrorCode.EvidenceSourceTransientException, detailErrorCode, message)
    {
    }

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    /// <param name="message">
    /// Error message
    /// </param>
    /// <param name="innerException">
    /// Inner exception
    /// </param>
    public EvidenceSourceTransientException(int detailErrorCode, string message, Exception innerException) : base(ErrorCode.EvidenceSourceTransientException, detailErrorCode, message, innerException)
    {
    }
}