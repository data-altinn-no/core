using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Deleted Subject Exception
/// FIXME! This is not used anywhere
/// </summary>
public class DeletedSubjectException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The subject requested is flagged as deleted in the Central Coordinating Register for Legal Entities";

    /// <summary>
    /// Deleted Subject Exception
    /// </summary>
    public DeletedSubjectException() : base(ErrorCode.DeletedSubjectException)
    {
    }

    /// <summary>
    /// Deleted Subject Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public DeletedSubjectException(string? message) : base(ErrorCode.DeletedSubjectException, message)
    {
    }

    /// <summary>
    /// Deleted Subject Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public DeletedSubjectException(string? message, Exception? innerException) : base(ErrorCode.DeletedSubjectException, message, innerException)
    {
    }
}