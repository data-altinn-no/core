using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Source Not Available Exception
/// </summary>
public class ServiceNotAvailableException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "There was an error during an attempt to access a upstream source. The remote server might be down or overloaded";

    /// <summary>
    /// Source Not Available Exception
    /// </summary>
    public ServiceNotAvailableException() : base(ErrorCode.ServiceNotAvailableException)
    {
    }

    /// <summary>
    /// Source Not Available Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public ServiceNotAvailableException(string? message) : base(ErrorCode.ServiceNotAvailableException, message)
    {
    }

    /// <summary>
    /// Source Not Available Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ServiceNotAvailableException(string? message, Exception? innerException) : base(ErrorCode.ServiceNotAvailableException, message, innerException)
    {
    }
}