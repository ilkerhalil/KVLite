using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SystemCacheItemPolicy = System.Runtime.Caching.CacheItemPolicy;
using SystemMemoryCache = System.Runtime.Caching.MemoryCache;

namespace PommaLabs.KVLite
{
    public sealed class MemoryCache : AbstractCache<MemoryCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        [Pure]
        public static MemoryCache DefaultInstance { get; } = new MemoryCache(new MemoryCacheSettings());

        #endregion Default Instance

        #region Construction

        SystemMemoryCache _store;

        /// <summary>
        ///   Initializes a new instance of the <see cref="PersistentCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        public MemoryCache(MemoryCacheSettings settings, ILog log = null, ISerializer serializer = null)
        {
            Settings = settings;
            Log = log ?? LogManager.GetLogger(GetType());
            Serializer = serializer ?? new JsonSerializer(new JsonSerializerSettings
            {
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.Ignore,
                Formatting = Newtonsoft.Json.Formatting.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.None
            });

            InitSystemMemoryCache();
        }

        #endregion Construction

        public override IClock Clock { get; } = new SystemClock();

        public override ICompressor Compressor { get; } = new NoOpCompressor();

        public override ILog Log { get; }

        public override ISerializer Serializer { get; }

        public override MemoryCacheSettings Settings { get; }

        /// <summary>
        ///   True if the Peek methods are implemented, false otherwise.
        /// </summary>
        public override bool CanPeek => false;

        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval)
        {
            var policy = (interval == TimeSpan.Zero)
                ? new SystemCacheItemPolicy { AbsoluteExpiration = utcExpiry }
                : new SystemCacheItemPolicy { SlidingExpiration = interval };

            _store.Set(SerializeToCacheKey(partition, key), value, policy);
        }

        protected override void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            // We need to make a snapshot of the keys, since the cache might be used by other
            // processes. Therefore, we start projecting all keys.
            var keys = _store.Select(x => x.Key);

            // Then, if a partition has been specified, we select only those keys that belong to
            // that partition.
            if (partition != null)
            {
                keys = keys.Where(k => DeserializeFromCacheKey(k).Partition == partition);
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
            var maybeCacheKey = SerializeToCacheKey(partition, key);
            return _store.Contains(maybeCacheKey);
        }

        protected override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            // If partition has not been specified, then we use the GetCount method provided
            // directly by the MemoryCache.
            if (partition == null)
            {
                return _store.GetCount();
            }

            // Otherwise, we need to count items, which is surely slower. In fact, we also need to
            // deserialize the key in order to understand if the item belongs to the partition.
            return _store.Count(x => DeserializeFromCacheKey(x.Key).Partition == partition);
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            var maybeCacheKey = SerializeToCacheKey(partition, key);
            var maybeValue = _store.Get(maybeCacheKey);

            // If item is not present or if it has the wrong type, return None.
            if (maybeValue == null || !(maybeValue is TVal))
            {
                return Option.None<TVal>();
            }

            // Otherwise, cast the item and return it.
            return Option.Some((TVal) maybeValue);
        }

        protected override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            var maybeCacheKey = SerializeToCacheKey(partition, key);
            var maybeCacheItem = _store.GetCacheItem(maybeCacheKey);

            // If item is not present or if it has the wrong type, return None.
            if (maybeCacheItem == null || !(maybeCacheItem.Value is TVal))
            {
                return Option.None<CacheItem<TVal>>();
            }

            // Otherwise, generate the KVLite cache item and return it. Many properties available in
            // the KVLite cache items cannot be filled due to missing information.
            return Option.Some(new CacheItem<TVal>
            {
                Partition = partition,
                Key = key,
                Value = (TVal) maybeCacheItem.Value
            });
        }

        protected override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            // Pick only the items with the right type.
            var q = _store.Where(x => x.Value is TVal).Select(x => new
            {
                CacheKey = DeserializeFromCacheKey(x.Key),
                Value = (TVal) x.Value
            });

            // If partition has been specified, then we shall also filter by it.
            if (partition != null)
            {
                q = q.Where(x => x.CacheKey.Partition == partition);
            }

            // Project the items to proper KVLite cache items. Many properties available in
            // the KVLite cache items cannot be filled due to missing information.
            return q.Select(x => new CacheItem<TVal>
            {
                Partition = x.CacheKey.Partition,
                Key = x.CacheKey.Key,
                Value = x.Value
            }).ToArray();
        }

        protected override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
        }

        protected override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
        }

        protected override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
        }

        protected override void RemoveInternal(string partition, string key)
        {
            var maybeCacheKey = SerializeToCacheKey(partition, key);
            _store.Remove(maybeCacheKey);
        }

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.CacheName):
                    InitSystemMemoryCache();
                    break;
            }
        }

        void InitSystemMemoryCache()
        {
            _store = new SystemMemoryCache(Settings.CacheName);
        }

        #region Cache size estimation

        public double CacheSizeInKB()
        {
            var serializer = new BinarySerializer();
            var cacheItems = _store.ToArray();
            using (var stream = serializer.SerializeToStream(cacheItems))
            {
                return stream.Length / 1024.0;
            }
        }

        #endregion

        #region Cache key handling

        struct CacheKey
        {
            public string Partition { get; set; }

            public string Key { get; set; }
        }

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        string SerializeToCacheKey(string partition, string key) => Serializer.SerializeToString(new CacheKey
        {
            Partition = partition,
            Key = key
        });

#if NET45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        CacheKey DeserializeFromCacheKey(string cacheKey) => Serializer.DeserializeFromString<CacheKey>(cacheKey);

        #endregion Cache key handling
    }
}
