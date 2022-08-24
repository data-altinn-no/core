using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Expired Accreditation Exception
/// </summary>
public class ExpiredAccreditationException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The accreditation requested has expired and is no longer valid";

    /// <summary>
    /// Expired Accreditation Exception
    /// </summary>
    public ExpiredAccreditationException() : base(ErrorCode.ExpiredAccreditationException)
    {
    }

    /// <summary>
    /// Expired Accreditation Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public ExpiredAccreditationException(string? message) : base(ErrorCode.ExpiredAccreditationException, message)
    {
    }

    /// <summary>
    /// Expired Accreditation Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ExpiredAccreditationException(string? message, Exception? innerException) : base(ErrorCode.ExpiredAccreditationException, message, innerException)
    {
    }
}