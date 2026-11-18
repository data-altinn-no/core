using System.Diagnostics;
using System.Globalization;
using OpenTelemetry;

namespace Dan.Common.Telemetry;

/// <summary>
/// Masks Norwegian national identity numbers (11 digits) in trace spans before they are exported.
/// Many data sources put the subject's number in the URL path, so outgoing HTTP dependency spans would
/// otherwise carry it in url.full, url.path and the span name. Every tag on the span is masked (strings, and
/// integers with exactly 11 digits), as well as the span name. The first six digits are kept, matching
/// <see cref="Party.GetAsString"/>, so a span can still be correlated with the application log lines.
/// </summary>
/// <remarks>
/// Span events (e.g. recorded exceptions) and links are immutable once added and are not masked.
/// Log records are masked by <see cref="SsnRedactingLogProcessor"/>.
/// </remarks>
public sealed partial class SsnRedactingProcessor : BaseProcessor<Activity>
{
    // An 11-digit run not surrounded by other digits. A percent escape whose hex code ends in a digit
    // (e.g. %20, %27) also counts as a boundary, so URL-encoded numbers like "%2006815298657" are masked.
    // A match may not start inside a percent escape, so the escape's own digits are never counted as part
    // of the number ("%20991825827" is a 9-digit organization number and is left alone).
    [GeneratedRegex(@"(?:(?<=%[0-9A-Fa-f]{2})|(?<![0-9%])(?<!%[0-9A-Fa-f]))([0-9]{6})[0-9]{5}(?![0-9])")]
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

    /// <summary>
    /// Masks a telemetry value. Strings are masked with <see cref="Mask"/>. Integers with exactly 11 digits
    /// (e.g. an identity number logged as a <see cref="long"/>) and other objects whose string representation
    /// contains one (e.g. a <see cref="Uri"/>) are returned as a masked string. Any other value is returned
    /// unchanged, as the same instance.
    /// </summary>
    /// <param name="value">The value to mask</param>
    /// <returns>The masked value, or <paramref name="value"/> itself if nothing was masked</returns>
    public static object? MaskValue(object? value)
    {
        if (value is string s)
        {
            return Mask(s);
        }

        var digits = value switch
        {
            null => null,
            long l => l.ToString(CultureInfo.InvariantCulture),
            ulong ul => ul.ToString(CultureInfo.InvariantCulture),
            decimal d when decimal.Truncate(d) == d => d.ToString("0", CultureInfo.InvariantCulture),
            // Other primitives (int, double, bool, enums, DateTime, ...) cannot hold an 11-digit identity number.
            IConvertible => null,
            // Objects such as Uri are exported as their string representation, so mask that.
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };

        if (digits == null)
        {
            return value;
        }

        var masked = Mask(digits);
        return ReferenceEquals(masked, digits) ? value : masked;
    }

    /// <inheritdoc />
    public override void OnEnd(Activity activity)
    {
        HashSet<string>? affectedKeys = null;
        foreach (var tag in activity.TagObjects)
        {
            if (!ReferenceEquals(MaskValue(tag.Value), tag.Value))
            {
                (affectedKeys ??= new()).Add(tag.Key);
            }
        }

        // Tags cannot be changed while enumerating them, so they are updated afterwards. A key can occur more than
        // once (Activity.AddTag), and SetTag only touches the first occurrence, so every occurrence of an affected key
        // is removed and added back, in the original order, with its own masked value.
        if (affectedKeys != null)
        {
            var snapshot = activity.TagObjects.Where(t => affectedKeys.Contains(t.Key)).ToList();
            foreach (var key in affectedKeys)
            {
                while (activity.GetTagItem(key) != null)
                {
                    activity.SetTag(key, null);
                }
            }

            foreach (var tag in snapshot)
            {
                activity.AddTag(tag.Key, MaskValue(tag.Value));
            }
        }

        var displayName = Mask(activity.DisplayName)!;
        if (!ReferenceEquals(displayName, activity.DisplayName))
        {
            activity.DisplayName = displayName;
        }
    }
}
