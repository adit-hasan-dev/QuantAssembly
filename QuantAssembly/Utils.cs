namespace QuantAssembly.Utility
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Reflection;

    public static class Validator
    {
        /// <summary>
        /// Checks if all specified properties of an object are non-null.
        /// </summary>
        /// <param name="obj">The object to check.</param>
        /// <param name="propertyNames">A list of property names to check for non-null values.</param>
        /// <returns>True if all specified properties are non-null; otherwise, false.</returns>
        public static bool AssertPropertiesNonNull(object obj, List<string> propertyNames)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            if (propertyNames == null || propertyNames.Count == 0)
            {
                throw new ArgumentException("The list of property names cannot be null or empty.", nameof(propertyNames));
            }

            Type type = obj.GetType();

            foreach (string propertyName in propertyNames)
            {
                PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
                if (property == null)
                {
                    throw new ArgumentException($"Property '{propertyName}' does not exist on type '{type.Name}'.");
                }

                object value = property.GetValue(obj);
                if (value == null)
                {
                    return false;
                }
            }

            return true;
        }
    }

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