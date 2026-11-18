using OpenTelemetry;
using OpenTelemetry.Logs;

namespace Dan.Common.Telemetry;

/// <summary>
/// Masks Norwegian national identity numbers (11 digits) in log records before they are exported, using the same
/// rules as <see cref="SsnRedactingProcessor.Mask"/>. Covers the formatted message, the body (message template)
/// and every string attribute, so e.g. <c>logger.LogError("Call to {Url} failed", url)</c> is exported masked.
/// </summary>
/// <remarks>
/// Logging scopes and exception objects attached to a log record cannot be changed by a processor and are not masked.
/// Avoid putting identity numbers in scopes and exception messages, or mask them with
/// <see cref="SsnRedactingProcessor.Mask"/> before logging.
/// </remarks>
public sealed class SsnRedactingLogProcessor : BaseProcessor<LogRecord>
{
    /// <inheritdoc />
    public override void OnEnd(LogRecord data)
    {
        data.FormattedMessage = SsnRedactingProcessor.Mask(data.FormattedMessage);
        data.Body = SsnRedactingProcessor.Mask(data.Body);

        var attributes = data.Attributes;
        if (attributes == null)
        {
            return;
        }

        List<KeyValuePair<string, object?>>? masked = null;
        for (var i = 0; i < attributes.Count; i++)
        {
            var attribute = attributes[i];
            if (attribute.Value is string value)
            {
                var maskedValue = SsnRedactingProcessor.Mask(value);
                if (!ReferenceEquals(maskedValue, value))
                {
                    // Copy the attributes the first time something needs masking; the original list may be read-only.
                    masked ??= new List<KeyValuePair<string, object?>>(attributes);
                    masked[i] = new(attribute.Key, maskedValue);
                }
            }
        }

        if (masked != null)
        {
            data.Attributes = masked;
        }
    }
}
