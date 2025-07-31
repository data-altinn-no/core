using Dan.Common.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace Dan.Common.Services

{
    public class PluginMemoryCacheProvider : IPluginMemoryCacheProvider
    {
        private readonly IMemoryCache _memoryCache;

        public PluginMemoryCacheProvider(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;            
        }
        public Task<(bool success, T result)> TryGet<T>(string key)
        {
            bool success = _memoryCache.TryGetValue(key, out T result);
            return Task.FromResult<(bool, T)>((success, result));
        }

        public async Task<T> SetCache<T>(string key, T value, TimeSpan timeToLive)
        {
            MemoryCacheEntryOptions cacheEntryOptions = new MemoryCacheEntryOptions()
            {
                Priority = CacheItemPriority.High
            };

            cacheEntryOptions.SetAbsoluteExpiration(timeToLive);
            var result = _memoryCache.Set(key, value, cacheEntryOptions);

            await Task.CompletedTask;

            return result;
        }
    }
}
