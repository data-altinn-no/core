using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Evidence Request Exception
/// </summary>
public class InvalidEvidenceRequestParameterException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with one or more of the parameters given for one of the evidence codes requested";

    /// <summary>
    /// Invalid Evidence Request Exception
    /// </summary>
    public InvalidEvidenceRequestParameterException() : base(ErrorCode.InvalidEvidenceRequestParameterException)
    {
    }

    /// <summary>
    /// Invalid Evidence Request Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidEvidenceRequestParameterException(string message) : base(ErrorCode.InvalidEvidenceRequestParameterException, message)
    {
    }

    /// <summary>
    /// Invalid Evidence Request Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidEvidenceRequestParameterException(string message, Exception innerException) : base(ErrorCode.InvalidEvidenceRequestParameterException, message, innerException)
    {
    }
}