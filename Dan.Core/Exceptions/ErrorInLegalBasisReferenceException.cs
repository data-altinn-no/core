using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Error In Legal Basis Reference Exception
/// FIXME! This is not used anywhere
/// </summary>
public class ErrorInLegalBasisReferenceException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error in a legal basis reference for one or more of the evidence requests";

    /// <summary>
    /// Error In Legal Basis Reference Exception
    /// </summary>
    public ErrorInLegalBasisReferenceException() : base(ErrorCode.ErrorInLegalBasisReferenceException)
    {
    }

    /// <summary>
    /// Error In Legal Basis Reference Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public ErrorInLegalBasisReferenceException(string? message) : base(ErrorCode.ErrorInLegalBasisReferenceException, message)
    {
    }

    /// <summary>
    /// Error In Legal Basis Reference Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ErrorInLegalBasisReferenceException(string? message, Exception? innerException) : base(ErrorCode.ErrorInLegalBasisReferenceException, message, innerException)
    {
    }
}