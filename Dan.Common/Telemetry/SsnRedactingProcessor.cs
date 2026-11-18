using System.Diagnostics;
using OpenTelemetry;

namespace Dan.Common.Telemetry;

/// <summary>
/// Masks Norwegian national identity numbers (11 digits) in trace spans before they are exported.
/// Many data sources put the subject's number in the URL path, so outgoing HTTP dependency spans would
/// otherwise carry it in url.full, url.path and the span name. Every string tag on the span is masked, as well
/// as the span name. The first six digits are kept, matching <see cref="Party.GetAsString"/>, so a span can
/// still be correlated with the application log lines.
/// </summary>
/// <remarks>
/// Span events (e.g. recorded exceptions) and links are immutable once added and are not masked.
/// Log records are masked by <see cref="SsnRedactingLogProcessor"/>.
/// </remarks>
public sealed partial class SsnRedactingProcessor : BaseProcessor<Activity>
{
    // An 11-digit run not surrounded by other digits. A percent escape whose hex code ends in a digit
    // (e.g. %20, %27) also counts as a boundary, so URL-encoded numbers like "%2006815298657" are masked.
    [GeneratedRegex(@"(?:(?<=%[0-9A-Fa-f]{2})|(?<![0-9]))([0-9]{6})[0-9]{5}(?![0-9])")]
    private static partial Regex ElevenDigits();

    /// <summary>
    /// Replaces the last five digits of every free-standing 11-digit number in <paramref name="value"/> with asterisks.
    /// Shorter or longer digit sequences (organization numbers, sequence numbers) are left unchanged.
    /// </summary>
    /// <param name="value">The text to mask</param>
    /// <returns>
    /// The masked text. If nothing was masked (or the input is null or empty), the same string instance is returned,
    /// so callers can use <see cref="object.ReferenceEquals"/> to detect a change.
    /// </returns>
    public static string? Mask(string? value)
    {
        return string.IsNullOrEmpty(value) ? value : ElevenDigits().Replace(value, "$1*****");
    }

    /// <inheritdoc />
    public override void OnEnd(Activity activity)
    {
        List<KeyValuePair<string, string>>? masked = null;
        foreach (var tag in activity.TagObjects)
        {
            if (tag.Value is string value)
            {
                var maskedValue = Mask(value)!;
                if (!ReferenceEquals(maskedValue, value))
                {
                    (masked ??= new()).Add(new(tag.Key, maskedValue));
                }
            }
        }

        // Tags cannot be changed while enumerating them, so they are updated afterwards.
        if (masked != null)
        {
            foreach (var tag in masked)
            {
                activity.SetTag(tag.Key, tag.Value);
            }
        }

        var displayName = Mask(activity.DisplayName)!;
        if (!ReferenceEquals(displayName, activity.DisplayName))
        {
            activity.DisplayName = displayName;
        }
    }
}
