using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Invalid Certificate Exception
/// </summary>
public class InvalidCertificateException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error with the provided client certificate";

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    public InvalidCertificateException() : base(ErrorCode.InvalidCertificateException)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public InvalidCertificateException(string message) : base(ErrorCode.InvalidCertificateException, message)
    {
    }

    /// <summary>
    /// Invalid Certificate Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public InvalidCertificateException(string message, Exception innerException) : base(ErrorCode.InvalidCertificateException, message, innerException)
    {
    }
}