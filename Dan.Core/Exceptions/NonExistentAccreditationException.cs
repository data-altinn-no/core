using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Non-Existent Accreditation Exception
/// </summary>
public class NonExistentAccreditationException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The referenced accreditation does not exist";

    /// <summary>
    /// Non-Existent Accreditation Exception
    /// </summary>
    public NonExistentAccreditationException() : base(ErrorCode.NonExistentAccreditationException)
    {
    }

    /// <summary>
    /// Non-Existent Accreditation Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public NonExistentAccreditationException(string message) : base(ErrorCode.NonExistentAccreditationException, message)
    {
    }

    /// <summary>
    /// Non-Existent Accreditation Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public NonExistentAccreditationException(string message, Exception innerException) : base(ErrorCode.NonExistentAccreditationException, message, innerException)
    {
    }
}