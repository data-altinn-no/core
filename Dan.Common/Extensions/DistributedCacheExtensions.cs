using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Dan.Common.Extensions;

/// <summary>
/// Extensions for IDistributedCache
/// </summary>
public static class DistributedCacheExtensions
{
    /// <summary>
    /// Gets value from distributed cache, handles decoding from UTF8 bytes
    /// </summary>
    /// <param name="distributedCache">Target cache</param>
    /// <param name="key">Cache key</param>
    /// <typeparam name="T">Type that cached value should be deserialised into</typeparam>
    /// <returns>Deserialized value, or null if not found</returns>
    public static async Task<T?> GetValueAsync<T>(this IDistributedCache distributedCache, string key)
    {
        var encodedPoco = await distributedCache.GetAsync(key);
        if (encodedPoco == null)
        {
            return default;
        }
        var serializedPoco = Encoding.UTF8.GetString(encodedPoco);
        return JsonConvert.DeserializeObject<T>(serializedPoco);
    }
    
    /// <summary>
    /// Sets value by handling encoding to UTF8
    /// </summary>
    /// <param name="distributedCache">Target cache</param>
    /// <param name="key">Cache key</param>
    /// <param name="value">Cache value</param>
    /// <param name="options">Cache options</param>
    /// <typeparam name="T">Any class that can be serialised and encoded to UTF8</typeparam>
    public static async Task SetValueAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions? options = null)
    {
        options ??= new DistributedCacheEntryOptions();
        var serializedValue = JsonConvert.SerializeObject(value);
        var encodedValue = Encoding.UTF8.GetBytes(serializedValue);
        await distributedCache.SetAsync(key, encodedValue, options);
    }
}