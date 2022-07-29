using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

/// <summary>
/// Authorization Failed Exception
/// </summary>
public class ConsentAlreadyHandledException : DanException
{
    /// <inheritdoc />
    public override string DefaultErrorMessage => "The consent has already been given or rejected";

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    public ConsentAlreadyHandledException() : base(ErrorCode.ConsentAlreadyHandledException)
    {
    }

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    /// <param name="message">Error Message</param>
    public ConsentAlreadyHandledException(string message) : base(ErrorCode.ConsentAlreadyHandledException, message)
    {
    }

    /// <summary>
    /// Authorization Failed Exception
    /// </summary>
    /// <param name="message">Error message</param>
    /// <param name="innerException">Inner exception</param>
    public ConsentAlreadyHandledException(string message, Exception innerException) : base(ErrorCode.ConsentAlreadyHandledException, message, innerException)
    {
    }
}