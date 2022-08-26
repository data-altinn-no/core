using System.Text;

namespace Dan.Core.Helpers;

/// <summary>
/// Base64Url encode/decode functionality following the Base 64 URL standard
/// </summary>
public class Base64Url
{
    /// <summary>
    /// Encode a string using Base64Url
    /// </summary>
    /// <param name="str">The string</param>
    /// <returns>The encoded string</returns>
    public static string? Encode(string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        var bytesToEncode = Encoding.UTF8.GetBytes(str);
        var returnVal = Convert.ToBase64String(bytesToEncode);

        return returnVal.TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    /// <summary>
    /// Decodes a string using Base64Url
    /// </summary>
    /// <param name="str">The encoded string</param>
    /// <returns>The decoded string</returns>
    public static string? Decode(string? str)
    {
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }

        str = str.Replace('-', '+');
        str = str.Replace('_', '/');

        var paddings = str.Length % 4;
        if (paddings > 0)
        {
            str += new string('=', 4 - paddings);
        }

        var encodedDataAsBytes = Convert.FromBase64String(str);
        var returnVal = Encoding.UTF8.GetString(encodedDataAsBytes);
        return returnVal;
    }
}