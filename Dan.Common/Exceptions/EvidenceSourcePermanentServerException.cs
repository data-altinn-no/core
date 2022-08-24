namespace Dan.Common.Exceptions;

/// <summary>
/// Permanent evidence source server exception. Used when the evidence source is misconfigured or any other unexpected server side error occurred.
/// </summary>
public class EvidenceSourcePermanentServerException : EvidenceSourceException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "A permanent error occured serverside while requesting one of the evidence codes. This may be due to a misconfigured evidence source";

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    public EvidenceSourcePermanentServerException() : base(ErrorCode.EvidenceSourcePermanentServerException)
    {
    }

    /// <summary>
    /// Transient evidence source exception
    /// </summary>
    /// <param name="detailErrorCode">
    /// The detail Error Code.
    /// </param>
    public EvidenceSourcePermanentServerException(int detailErrorCode) : base(ErrorCode.EvidenceSourcePermanentServerException, detailErrorCode)
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
    public EvidenceSourcePermanentServerException(int detailErrorCode, string? message) : base(ErrorCode.EvidenceSourcePermanentServerException, detailErrorCode, message)
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
    public EvidenceSourcePermanentServerException(int detailErrorCode, string? message, Exception? innerException) : base(ErrorCode.EvidenceSourcePermanentServerException, detailErrorCode, message, innerException)
    {
    }
}