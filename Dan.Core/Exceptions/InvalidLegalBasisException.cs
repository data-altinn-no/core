using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Legal Basis Exception
/// </summary>
public class InvalidLegalBasisException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error in validating and/or processing one or more of the supplied legal basises";

    /// <summary>
    /// Invalid Legal Basis Exception
    /// </summary>
    public InvalidLegalBasisException() : base(ErrorCode.InvalidLegalBasisException)
    {
    }

    /// <summary>
    /// Invalid Legal Basis Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidLegalBasisException(string? message) : base(ErrorCode.InvalidLegalBasisException, message)
    {
    }

    /// <summary>
    /// Invalid Legal Basis Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidLegalBasisException(string? message, Exception? innerException) : base(ErrorCode.InvalidLegalBasisException, message, innerException)
    {
    }
}