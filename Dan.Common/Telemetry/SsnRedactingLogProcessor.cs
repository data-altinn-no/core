using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Dan.Common.Telemetry;

/// <summary>
/// Masks Norwegian national identity numbers (11 digits) in log records before they are exported, using the same
/// rules as <see cref="SsnRedactingProcessor.Mask"/>. Covers the formatted message, the body (message template),
/// every attribute (strings, and integers with exactly 11 digits) and the attached exception, so e.g.
/// <c>logger.LogError(ex, "Call to {Url} failed", url)</c> is exported masked.
/// </summary>
/// <remarks>
/// An exception whose message or stack trace contains an identity number is replaced by a
/// <see cref="RedactedException"/>, which keeps the original type name in its message and the original text
/// (masked) in <see cref="Exception.ToString"/>. Logging scopes cannot be changed by a processor; instead, a scope
/// value that needs masking gets a masked attribute with the same key, which the exporter uses in place of the
/// scope value. Requires <c>IncludeScopes</c>, which is how Dan.Common configures OpenTelemetry logging.
/// </remarks>
public sealed class SsnRedactingLogProcessor : BaseProcessor<LogRecord>
{
    /// <inheritdoc />
    public override void OnEnd(LogRecord data)
    {
        data.FormattedMessage = SsnRedactingProcessor.Mask(data.FormattedMessage);
        data.Body = SsnRedactingProcessor.Mask(data.Body);

        if (data.Exception != null)
        {
            data.Exception = RedactedException.MaskIfNeeded(data.Exception);
        }

        var attributes = data.Attributes ?? Array.Empty<KeyValuePair<string, object?>>();

        List<KeyValuePair<string, object?>>? masked = null;
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            var maskedValue = SsnRedactingProcessor.MaskValue(attribute.Value);
            if (!ReferenceEquals(maskedValue, attribute.Value))
            {
                // Copy the attributes the first time something needs masking; the original list may be read-only.
                masked ??= new List<KeyValuePair<string, object?>>(attributes);
                masked[i] = new(attribute.Key, maskedValue);
            }
        }

        // Scopes cannot be changed, but the exporter gives a record attribute precedence over a scope value with the
        // same key. So for every scope value that needs masking, add a masked attribute with that key.
        var scopeState = new ScopeState(attributes, masked);
        data.ForEachScope(static (scope, state) =>
        {
            foreach (var item in scope)
            {
                if (item.Key == "{OriginalFormat}" || state.HasKey(item.Key))
                {
                    continue;
                }

                var maskedValue = SsnRedactingProcessor.MaskValue(item.Value);
                if (!ReferenceEquals(maskedValue, item.Value))
                {
                    state.Masked ??= new List<KeyValuePair<string, object?>>(state.Attributes);
                    state.Masked.Add(new(item.Key, maskedValue));
                }
            }
        }, scopeState);
        masked = scopeState.Masked;

        if (masked != null)
        {
            data.Attributes = masked;
        }
    }

    private sealed class ScopeState(IReadOnlyList<KeyValuePair<string, object?>> attributes, List<KeyValuePair<string, object?>>? masked)
    {
        public IReadOnlyList<KeyValuePair<string, object?>> Attributes { get; } = attributes;
        public List<KeyValuePair<string, object?>>? Masked { get; set; } = masked;

        public bool HasKey(string key) => (Masked ?? Attributes).Any(a => a.Key == key);
    }
}
