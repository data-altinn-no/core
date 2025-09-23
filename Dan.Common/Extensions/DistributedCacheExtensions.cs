using System.Text;
using Microsoft.Extensions.Caching.Distributed;

namespace Dan.Common.Extensions;

/// <summary>
/// Extension methods for IDistributedCache (i.e Redis)
/// </summary>
public static class DistributedCacheExtensions
{
    /// <summary>
    /// Gets value from distributed cache by key and deserializes into POCO
    /// </summary>
    /// <typeparam name="T">Type to deserialize into</typeparam>
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
    /// Serializes and sets value in distributed cache
    /// </summary>
    public static async Task SetValueAsync<T>(this IDistributedCache distributedCache, string key, T value, DistributedCacheEntryOptions? options = null)
    {
        options ??= new DistributedCacheEntryOptions();
        var serializedValue = JsonConvert.SerializeObject(value);
        var encodedValue = Encoding.UTF8.GetBytes(serializedValue);
        await distributedCache.SetAsync(key, encodedValue, options);
    }
}