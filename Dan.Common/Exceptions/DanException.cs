namespace Dan.Common.Exceptions;

/// <summary>
/// Nadobe Exception
/// </summary>
public abstract class DanException : Exception
{
    /// <summary>
    /// Exception error code
    /// </summary>
    public ErrorCode ExceptionErrorCode { get; set; }

    /// <summary>
    /// Optional evidence source specific error code
    /// </summary>
    public int? DetailErrorCode { get; set; }

    /// <summary>
    /// Optional evidence source specific error description
    /// </summary>
    public string DetailErrorDescription { get; set; }

    /// <summary>
    /// What evidence source set the specific error
    /// </summary>
    public string DetailErrorSource { get; set; }

    /// <summary>
    /// Support stack traces coming from evidence sources. If set, will be used in stead on InnerException.StackTrace
    /// </summary>
    public string InnerStackTrace { get; set; }

    /// <summary>
    /// Default exception message which should be overridden for each subclass
    /// </summary>
    public virtual string DefaultErrorMessage
    {
        get
        {
            var type = GetType().Name;
            return new Regex("([a-z])([A-Z])").Replace(type, "$1 $2").Replace(" Exception", string.Empty);
        }
    }

    /// <summary>
    /// Exception message
    /// </summary>
    public override string Message { get; }

    /// <summary>
    /// Nadobe Exception
    /// </summary>
    /// <param name="errorCode">Error code</param>
    protected DanException(ErrorCode errorCode)
    {
        ExceptionErrorCode = errorCode;
        Message = DefaultErrorMessage;
    }

    /// <summary>
    /// Nadobe Exception
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="message">Error Message</param>
    protected DanException(ErrorCode errorCode, string message) : base(message)
    {
        ExceptionErrorCode = errorCode;
        Message = !String.IsNullOrEmpty(message) ? $"{DefaultErrorMessage}: {message}" : DefaultErrorMessage;
    }

    /// <summary>
    /// Nadobe Exception
    /// </summary>
    /// <param name="errorCode">Error code</param>
    /// <param name="message">Error Message</param>
    /// <param name="innerException">Inner exception</param>
    protected DanException(ErrorCode errorCode, string message, Exception innerException) : base(message, innerException)
    {
        ExceptionErrorCode = errorCode;
        Message = !String.IsNullOrEmpty(message) ? $"{DefaultErrorMessage}: {message}" : DefaultErrorMessage;
    }
}