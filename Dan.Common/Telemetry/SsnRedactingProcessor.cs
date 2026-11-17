using System.Diagnostics;
using OpenTelemetry;

namespace Dan.Common.Telemetry;

/// <summary>
/// Masks Norwegian national identity numbers (11 digits) in trace spans before they are exported.
/// Many data sources put the subject's number in the URL path, so outgoing HTTP dependency spans would
/// otherwise carry it in url.full, url.path and the span name. The first six digits are kept, matching
/// <see cref="Party.GetAsString"/>, so a span can still be correlated with the application log lines.
/// </summary>
public sealed class SsnRedactingProcessor : BaseProcessor<Activity>
{
    private static readonly string[] UrlTags = { "url.full", "url.path", "url.query", "http.url", "http.target" };

    private static readonly Regex ElevenDigits = new(@"(?<![0-9])([0-9]{6})[0-9]{5}(?![0-9])", RegexOptions.Compiled);

    /// <summary>
    /// Replaces the last five digits of every free-standing 11-digit number in <paramref name="value"/> with asterisks.
    /// Shorter or longer digit sequences (organization numbers, sequence numbers) are left unchanged.
    /// </summary>
    /// <param name="value">The text to mask</param>
    /// <returns>The masked text, or the input if it is null or empty</returns>
    public static string? Mask(string? value)
    {
        return string.IsNullOrEmpty(value) ? value : ElevenDigits.Replace(value, "$1*****");
    }

    /// <inheritdoc />
    public override void OnEnd(Activity activity)
    {
        foreach (var tagName in UrlTags)
        {
            if (activity.GetTagItem(tagName) is string value && ElevenDigits.IsMatch(value))
            {
                activity.SetTag(tagName, Mask(value));
            }
        }

        if (ElevenDigits.IsMatch(activity.DisplayName))
        {
            activity.DisplayName = Mask(activity.DisplayName)!;
        }
    }
}
