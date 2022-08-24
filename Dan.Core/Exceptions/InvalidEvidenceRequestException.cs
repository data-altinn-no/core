using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Certificate Exception
/// </summary>
public class InvalidEvidenceRequestException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with one or more of the evidence codes requested";

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    public InvalidEvidenceRequestException() : base(ErrorCode.InvalidEvidenceRequestException)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidEvidenceRequestException(string? message) : base(ErrorCode.InvalidEvidenceRequestException, message)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidEvidenceRequestException(string? message, Exception? innerException) : base(ErrorCode.InvalidEvidenceRequestException, message, innerException)
    {
    }
}