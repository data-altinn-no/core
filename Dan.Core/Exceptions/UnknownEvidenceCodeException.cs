using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Unknown Evidence Code Exception
/// </summary>
public class UnknownEvidenceCodeException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "One or more of the evidence codes requested does not exist";

    /// <summary>
    /// Unknown Evidence Code Exception
    /// </summary>
    public UnknownEvidenceCodeException() : base(ErrorCode.UnknownEvidenceCodeException)
    {
    }

    /// <summary>
    /// Unknown Evidence Code Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public UnknownEvidenceCodeException(string message) : base(ErrorCode.UnknownEvidenceCodeException, message)
    {
    }

    /// <summary>
    /// Unknown Evidence Code Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public UnknownEvidenceCodeException(string message, Exception innerException) : base(ErrorCode.UnknownEvidenceCodeException, message, innerException)
    {
    }
}