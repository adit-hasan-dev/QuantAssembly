namespace QuantAssembly.Utility
{
    using System;
    using System.Collections.Concurrent;

    public static class CacheWrapper
    {
        private static readonly ConcurrentDictionary<string, CacheItem<object>> _cache = new ConcurrentDictionary<string, CacheItem<object>>();

        public static async Task<T> WithCacheAsync<T>(string key, Func<Task<T>> getDataFunc, TimeSpan? ttl = null)
        {
            if (_cache.TryGetValue(key, out var cacheItem) && cacheItem.Expiration > DateTime.Now)
            {
                return (T)cacheItem.Value;
            }

            var data = await getDataFunc();
            var expiration = DateTime.Now.Add(ttl ?? TimeSpan.FromMinutes(30));
            _cache[key] = new CacheItem<object> { Value = data, Expiration = expiration };

            return data;
        }

        private class CacheItem<TValue>
        {
            public TValue Value { get; set; }
            public DateTime Expiration { get; set; }
        }
    }


}