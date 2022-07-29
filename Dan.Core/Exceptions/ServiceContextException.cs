using Dan.Common.Enums;
using Dan.Common.Exceptions;

namespace Dan.Core.Exceptions;

public class ServiceContextException : DanException
{

    public override string DefaultErrorMessage => "ServiceContext is not configured properly.";


    public ServiceContextException() : base(ErrorCode.ServiceContextException)
    {
    }

    /// <summary>
    /// ServiceContextException
    /// </summary>
    /// <param name="message">Error Message</param>
    public ServiceContextException(string message) : base(ErrorCode.ServiceContextException, message) { }
    /// <summary>
    /// ServiceContextException
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="message">Error Message</param>
    /// <param name="innerException">Inner exception</param>
    public ServiceContextException(string message, Exception innerException) : base(ErrorCode.ServiceContextException, message, innerException) { }


}