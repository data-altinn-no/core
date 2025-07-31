using System.Collections.Generic;
using System.Threading.Tasks;
using Dan.Common.Models;

namespace Dan.Common.Interfaces;

    public interface IPluginMemoryCacheProvider
    {
        public Task<(bool success, T result)> TryGet<T>(string key);

        public Task<T> SetCache<T>(string key, T model, TimeSpan timeToLive);
    }

