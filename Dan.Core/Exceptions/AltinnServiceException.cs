using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Asynchronous Evidence Still Waiting Exception
/// </summary>
public class AltinnServiceException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was a failure in communication with Altinn.";

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    public AltinnServiceException() : base(ErrorCode.AltinnServiceException)
    {
    }

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public AltinnServiceException(string message) : base(ErrorCode.AltinnServiceException, message)
    {
    }

    /// <summary>
    /// Asynchronous Evidence Still Waiting Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public AltinnServiceException(string message, Exception innerException) : base(ErrorCode.AltinnServiceException, message, innerException)
    {
    }
}