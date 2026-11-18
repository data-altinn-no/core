namespace Dan.Common.Telemetry;

/// <summary>
/// Stand-in for an exception whose message or stack trace contains a national identity number. Exporters read
/// <see cref="Exception.Message"/> and <see cref="Exception.ToString"/> directly from the exception object, which a
/// log processor cannot change, so <see cref="SsnRedactingLogProcessor"/> replaces the exception on the log record
/// with this masked copy. The original exception type is kept at the start of <see cref="Message"/> and in
/// <see cref="OriginalType"/>, and <see cref="ToString"/> returns the original exception's full text (type, message,
/// inner exceptions and stack trace), masked.
/// </summary>
public sealed class RedactedException : Exception
{
    private readonly string _toString;
    private readonly string? _stackTrace;

    private RedactedException(string message, string toString, string? stackTrace, string originalType, Exception? innerException)
        : base(message, innerException)
    {
        _toString = toString;
        _stackTrace = stackTrace;
        OriginalType = originalType;
    }

    /// <summary>
    /// The full type name of the exception that was masked.
    /// </summary>
    public string OriginalType { get; }

    /// <inheritdoc />
    public override string? StackTrace => _stackTrace;

    /// <inheritdoc />
    public override string ToString() => _toString;

    /// <summary>
    /// Returns a masked copy of <paramref name="exception"/> if its text contains a national identity number,
    /// otherwise <paramref name="exception"/> itself (the same instance).
    /// </summary>
    /// <param name="exception">The exception to check</param>
    /// <returns>The masked copy, or the original exception if nothing needed masking</returns>
    public static Exception MaskIfNeeded(Exception exception)
    {
        string fullText;
        try
        {
            fullText = exception.ToString();
        }
        catch
        {
            // A broken ToString() override must not break logging; fall back to the message.
            fullText = exception.Message;
        }

        if (ReferenceEquals(SsnRedactingProcessor.Mask(fullText), fullText))
        {
            return exception;
        }

        var originalType = exception.GetType().FullName ?? exception.GetType().Name;
        var inner = exception.InnerException == null ? null : MaskIfNeeded(exception.InnerException);

        return new RedactedException(
            $"{originalType}: {SsnRedactingProcessor.Mask(exception.Message)}",
            SsnRedactingProcessor.Mask(fullText)!,
            SsnRedactingProcessor.Mask(exception.StackTrace),
            originalType,
            inner);
    }
}
