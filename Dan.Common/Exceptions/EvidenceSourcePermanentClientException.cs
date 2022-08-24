namespace Dan.Common.Exceptions;

/// <summary>
/// Permanent evidence source client exception. Used when the evidence source cannot return data for the provided organization number and/or parameters.
/// </summary>
public class EvidenceSourcePermanentClientException : EvidenceSourceException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was a permanent error with the input to one the evidence codes requested. The evidence source cannot process this request with the provided parameters";

    /// <summary>
    /// Permanent evidence source exception
    /// </summary>
    public EvidenceSourcePermanentClientException() : base(ErrorCode.EvidenceSourcePermanentClientException)
    {
    }

    /// <summary>
    /// Permanent evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    public EvidenceSourcePermanentClientException(int detailErrorCode) : base(ErrorCode.EvidenceSourcePermanentClientException, detailErrorCode)
    {
    }

    /// <summary>
    /// Permanent evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    /// <param name="message">
    /// Error Message
    /// </param>
    public EvidenceSourcePermanentClientException(int detailErrorCode, string? message) : base(ErrorCode.EvidenceSourcePermanentClientException, detailErrorCode, message)
    {
    }

    /// <summary>
    /// Permanent evidence source exception
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
    public EvidenceSourcePermanentClientException(int detailErrorCode, string? message, Exception? innerException) : base(ErrorCode.EvidenceSourcePermanentClientException, detailErrorCode, message, innerException)
    {
    }
}