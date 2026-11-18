namespace Dan.Common.Telemetry;

/// <summary>
/// Stand-in for an exception whose message contains a national identity number. Exporters read
/// <see cref="Exception.Message"/> (and that of every inner exception) directly from the exception object, which a
/// log processor cannot change, so <see cref="SsnRedactingLogProcessor"/> replaces the exception on the log record
/// with this masked copy. The original exception type is kept at the start of <see cref="Message"/> and in
/// <see cref="OriginalType"/>, and <see cref="ToString"/> returns the original exception's full text (type, message,
/// inner exceptions and stack trace), masked.
/// </summary>
/// <remarks>
/// The Azure Monitor exporter takes the exception type and the parsed stack trace from the exception object itself,
/// so in Application Insights a replaced exception shows up as <see cref="RedactedException"/> without a parsed stack.
/// Only exceptions that actually carry an identity number in a message are replaced, and
/// <see cref="SsnRedactingLogProcessor"/> adds the masked full text as the <c>exception.original</c> log attribute so
/// the stack trace is still available.
/// </remarks>
internal sealed class RedactedException : Exception
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
    /// Returns a masked copy of <paramref name="exception"/> if its message, or the message of any of its inner
    /// exceptions, contains a national identity number. Otherwise returns <paramref name="exception"/> itself (the
    /// same instance).
    /// </summary>
    /// <remarks>
    /// Stack frames never hold data values, so only messages are checked. An exception whose own message is clean is
    /// still replaced if one of its inner exceptions needs masking, because the inner exception of the original object
    /// cannot be swapped out. Inner exceptions without an identity number are kept as they are.
    /// </remarks>
    /// <param name="exception">The exception to check</param>
    /// <returns>The masked copy, or the original exception if nothing needed masking</returns>
    public static Exception MaskIfNeeded(Exception exception)
    {
        if (!NeedsMasking(exception))
        {
            return exception;
        }

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

        var originalType = exception.GetType().FullName ?? exception.GetType().Name;
        var inner = exception.InnerException == null ? null : MaskIfNeeded(exception.InnerException);

        return new RedactedException(
            $"{originalType}: {SsnRedactingProcessor.Mask(exception.Message)}",
            SsnRedactingProcessor.Mask(fullText)!,
            SsnRedactingProcessor.Mask(exception.StackTrace),
            originalType,
            inner);
    }

    private static bool NeedsMasking(Exception exception)
    {
        var message = exception.Message;
        if (!ReferenceEquals(SsnRedactingProcessor.Mask(message), message))
        {
            return true;
        }

        // The exporter also exports every inner exception of an AggregateException.
        if (exception is AggregateException aggregate)
        {
            return aggregate.InnerExceptions.Any(NeedsMasking);
        }

        return exception.InnerException != null && NeedsMasking(exception.InnerException);
    }
}
