using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;

namespace Dan.Core.Helpers;

/// <summary>
/// Encodes values for the format they are placed in, so that they are shown as text and never interpreted as markup
/// </summary>
public static class OutputEncoder
{
    // Encodes the characters that are significant in HTML and leaves letters such as æ, ø and å as they are
    private static readonly HtmlEncoder HtmlTextEncoder = HtmlEncoder.Create(UnicodeRanges.All);

    private const string MarkdownSpecialCharacters = "\\`*_[]<>";

    /// <summary>
    /// Encodes a value for use in HTML text or in a quoted HTML attribute
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The HTML encoded value, or an empty string if the value is null</returns>
    public static string Html(string? value)
    {
        return string.IsNullOrEmpty(value) ? string.Empty : HtmlTextEncoder.Encode(value);
    }

    /// <summary>
    /// Escapes the characters that can start inline Markdown or HTML markup (links, images, emphasis, code and tags)
    /// </summary>
    /// <param name="value">The value</param>
    /// <returns>The escaped value, or an empty string if the value is null</returns>
    public static string Markdown(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
        {
            if (MarkdownSpecialCharacters.Contains(character))
            {
                builder.Append('\\');
            }

            builder.Append(character);
        }

        return builder.ToString();
    }
}
