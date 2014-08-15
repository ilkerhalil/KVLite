using System;
using System.Text;
using System.Web;
using System.Web.Caching;
using KVLite.Core;
using KVLite.Properties;

namespace KVLite.Web
{
    public sealed class HttpRuntimeCache : ICache<HttpRuntimeCache>
    {
        private static readonly Cache HttpCache = HttpRuntime.Cache ?? new Cache();

        private readonly BinarySerializer _binarySerializer = new BinarySerializer();

        public object this[string partition, string key]
        {
            get { return Get(partition, key); }
        }

        public object this[string key]
        {
            get { return Get(key); }
        }

        public object AddStatic(string partition, string key, object value)
        {
            var serializedKey = _binarySerializer.SerializeObject(Tuple.Create(partition, key));
            var serializedValue = _binarySerializer.SerializeObject(value);
            HttpCache.Add(Encoding.Default.GetString(serializedKey), serializedValue, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
            return value;
        }

        public object AddStatic(string key, object value)
        {
            return AddStatic(Settings.Default.DefaultPartition, key, value);
        }

        public void Clear()
        {
            Clear(CacheReadMode.ConsiderExpirationDate);
        }

        public void Clear(CacheReadMode cacheReadMode)
        {
            throw new System.NotImplementedException();
        }

        public int Count()
        {
            return (int) LongCount(CacheReadMode.ConsiderExpirationDate);
        }

        public int Count(CacheReadMode cacheReadMode)
        {
            return (int) LongCount(cacheReadMode);
        }

        public long LongCount()
        {
            return LongCount(CacheReadMode.ConsiderExpirationDate);
        }

        public long LongCount(CacheReadMode cacheReadMode)
        {
            throw new System.NotImplementedException();
        }

        public object Get(string partition, string key)
        {
            throw new NotImplementedException();
        }

        public object Get(string key)
        {
            return Get(Settings.Default.DefaultPartition, key);
        }

        public CacheItem GetItem(string partition, string key)
        {
            throw new NotImplementedException();
        }

        public CacheItem GetItem(string key)
        {
            return GetItem(Settings.Default.DefaultPartition, key);
        }
    }
}
