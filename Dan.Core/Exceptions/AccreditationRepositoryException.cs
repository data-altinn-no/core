using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Accreditation Repository Exception
/// </summary>
public class AccreditationRepositoryException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error accessing the accreditation repository";

    /// <summary>
    /// Accreditation Repository Exception
    /// </summary>
    public AccreditationRepositoryException() : base(ErrorCode.AccreditationRepositoryException)
    {
    }

    /// <summary>
    /// Accreditation Repository Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public AccreditationRepositoryException(string message) : base(ErrorCode.AccreditationRepositoryException, message)
    {
    }

    /// <summary>
    /// Accreditation Repository Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public AccreditationRepositoryException(string message, Exception innerException) : base(ErrorCode.AccreditationRepositoryException, message, innerException)
    {
    }
}