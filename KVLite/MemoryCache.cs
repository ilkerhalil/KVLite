using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using System.Runtime.CompilerServices;
using System.ComponentModel;
using SystemMemoryCache = System.Runtime.Caching.MemoryCache;
using SystemCacheItem = System.Runtime.Caching.CacheItem;
using SystemCacheItemPolicy = System.Runtime.Caching.CacheItemPolicy;

namespace PommaLabs.KVLite
{
    class MemoryCache : AbstractCache<MemoryCacheSettings>
    {
        SystemMemoryCache _store;

        public override IClock Clock
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ICompressor Compressor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ILog Log
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ISerializer Serializer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override MemoryCacheSettings Settings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval)
        {
            var policy = (interval == TimeSpan.Zero)
                ? new SystemCacheItemPolicy { AbsoluteExpiration = utcExpiry }
                : new SystemCacheItemPolicy { SlidingExpiration = interval };

            _store.Add(SerializeCacheKey(partition, key), value, policy);
        }

        protected override void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            // We need to make a snapshot of the keys, since the cache might be used by other processes.
            // Therefore, we start projecting all keys.
            var keys = _store.Select(x => x.Key);

            // Then, if a partition has been specified, we select only those keys that belong to that partition.
            if (partition != null)
            {
                keys = keys.Where(k => DeserializeCacheKey(k).Partition == partition);
            }

            // Now we take the snapshot of the keys.
            var keysArray = keys.ToArray();
            
            // At last, we can remove them safely from the store itself.
            foreach (var key in keys)
            {
                _store.Remove(key);
            }            
        }

        protected override bool ContainsInternal(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            throw new NotImplementedException();
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        protected override void RemoveInternal(string partition, string key)
        {
            throw new NotImplementedException();
        }

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.CacheName):
                    _store = new SystemMemoryCache(Settings.CacheName);
                    break;
            }
        }

        #region Cache key handling

        struct CacheKey
        {
            public string Partition { get; set; }

            public string Key { get; set; }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        string SerializeCacheKey(string partition, string key) => Serializer.SerializeToString(new CacheKey
        {
            Partition = partition,
            Key = key
        });

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        CacheKey DeserializeCacheKey(string cacheKey) => Serializer.DeserializeFromString<CacheKey>(cacheKey);

        #endregion
    }
}
