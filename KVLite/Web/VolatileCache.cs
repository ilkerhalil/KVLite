using System;
using System.Text;
using System.Web;
using System.Web.Caching;
using KVLite.Core;
using KVLite.Properties;

namespace KVLite.Web
{
    public sealed class VolatileCache : ICache<VolatileCache>
    {
        private static readonly Cache HttpCache = HttpRuntime.Cache ?? new Cache();

        private readonly BinarySerializer _binarySerializer = new BinarySerializer();

        public object AddPersistent(string partition, string key, object value)
        {
            var serializedKey = _binarySerializer.SerializeObject(Tuple.Create(partition, key));
            var serializedValue = _binarySerializer.SerializeObject(value);
            HttpCache.Add(Encoding.Default.GetString(serializedKey), serializedValue, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
            return value;
        }

        public object AddPersistent(string key, object value)
        {
            return AddPersistent(Settings.Default.DefaultPartition, key, value);
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
    }
}
