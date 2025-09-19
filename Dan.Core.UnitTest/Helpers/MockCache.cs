using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;

namespace Dan.Core.UnitTest.Helpers;

public class MockCache : IDistributedCache
    {
        private Dictionary<string, byte[]> _backingStore;

        public MockCache()
        {
            _backingStore = new Dictionary<string, byte[]>();
        }

        public byte[] Get(string key)
        {
            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_backingStore[key]));
        }

        public async Task<byte[]?> GetAsync(string key, CancellationToken token = new())
        {
            if (!_backingStore.ContainsKey(key))
            {
                return default;
            }
            return await Task.FromResult(_backingStore[key]);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            _backingStore[key] = value;
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new())
        {
            _backingStore[key] = value;
            await Task.CompletedTask;
        }

        public void Refresh(string key)
        {
            throw new NotImplementedException();
        }

        public Task RefreshAsync(string key, CancellationToken token = new())
        {
            throw new NotImplementedException();
        }

        public void Remove(string key)
        {
            throw new NotImplementedException();
        }

        public Task RemoveAsync(string key, CancellationToken token = new())
        {
            throw new NotImplementedException();
        }
    }