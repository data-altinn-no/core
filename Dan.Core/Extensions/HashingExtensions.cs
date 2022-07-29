using Newtonsoft.Json;
using System.Security.Cryptography;
using System.Text;

namespace Dan.Core.Extensions;

/// <summary>
/// Object extensions
/// </summary>
public static class HashingExtensions
{
    /// <summary>
    /// SHA256 hash of a string
    /// </summary>
    /// <param name="value">string value to hash</param>
    /// <returns>SHA256 hash of the string</returns>
    public static string Sha256(this string value)
    {
        using (SHA256 hash = SHA256.Create())
        {
            return String.Concat(hash
                .ComputeHash(Encoding.UTF8.GetBytes(value))
                .Select(item => item.ToString("x2")));
        }
    }

    /// <summary>
    /// SHA256 hash of a serialized object
    /// </summary>
    /// <param name="value">object to hash</param>
    /// <returns>SHA256 hash of the serialized object</returns>
    public static string SerializedSha256(this object value)
    {
        var serializedValue = JsonConvert.SerializeObject(value);
        return Sha256(serializedValue);
    }
}