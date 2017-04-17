// File name: MemoryCache.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using CodeProject.ObjectPool.Specialized;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Resources;
using PommaLabs.Thrower.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using SystemCacheItemPolicy = System.Runtime.Caching.CacheItemPolicy;
using SystemMemoryCache = System.Runtime.Caching.MemoryCache;

namespace PommaLabs.KVLite.Memory
{
    /// <summary>
    ///   An in-memory cache based on the .NET <see cref="SystemMemoryCache"/>.
    /// </summary>
    public sealed class MemoryCache : AbstractCache<MemoryCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
#pragma warning disable CC0022 // Should dispose object

        [Pure]
        public static MemoryCache DefaultInstance { get; } = new MemoryCache(new MemoryCacheSettings());

#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        #region Construction

        /// <summary>
        ///   The system memory cache used as backend.
        /// </summary>
        private SystemMemoryCache _store;

        /// <summary>
        ///   Initializes a new instance of the <see cref="MemoryCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamPool">The memory stream pool.</param>
        public MemoryCache(MemoryCacheSettings settings, ISerializer serializer = null, ICompressor compressor = null, IMemoryStreamPool memoryStreamPool = null)
        {
            Settings = settings;
            Compressor = compressor ?? DeflateCompressor.Instance;
            Serializer = serializer ?? JsonSerializer.Instance;
            MemoryStreamPool = memoryStreamPool ?? CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance;
            Clock = SystemClock.Instance;

            InitSystemMemoryCache();
        }

        #endregion Construction

        #region FormattableObject members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="object.ToString"/>.
        /// </summary>
        protected override IEnumerable<KeyValuePair<string, object>> GetFormattingMembers()
        {
            yield return new KeyValuePair<string, object>("SystemMemoryCacheName", _store.Name);
        }

        #endregion FormattableObject members

        #region IDisposable members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if it is a managed dispose, false otherwise.</param>
        /// <remarks>
        ///   When called on a cache using <see cref="SystemMemoryCache.Default"/> as store, this
        ///   method does nothing. In fact, it is not safe to dispose the default memory cache instance.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                // Nothing to do, we can handle only managed Dispose calls.
                return;
            }

            if (_store != null && _store != SystemMemoryCache.Default)
            {
                _store.Dispose();
                _store = null;
            }
        }

        #endregion IDisposable members

        #region ICache members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <remarks>
        ///   Since <see cref="SystemMemoryCache"/> does not allow clock customisation, then this
        ///   property defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public override IClock Clock { get; }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        public override ICompressor Compressor { get; }

        /// <summary>
        ///   The maximum number of parent keys each item can have. The .NET memory cache supports an
        ///   unlimited number of parent keys per item.
        /// </summary>
        public override int MaxParentKeyCountPerItem { get; } = int.MaxValue;

        /// <summary>
        ///   The pool used to retrieve <see cref="MemoryStream"/> instances.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance"/>.
        /// </remarks>
        public override IMemoryStreamPool MemoryStreamPool { get; }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>.
        /// </remarks>
        public override ISerializer Serializer { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        /// <value>The available settings for the cache.</value>
        public override MemoryCacheSettings Settings { get; }

        /// <summary>
        ///   <c>true</c> if the Peek methods are implemented, <c>false</c> otherwise.
        /// </summary>
        public override bool CanPeek => false;

        /// <summary>
        ///   Adds given value with the specified expiry time and refresh internal.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry time.</param>
        /// <param name="interval">The refresh interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            byte[] serializedValue;
            bool compressed;
            try
            {
                using (var serializedStream = MemoryStreamPool.GetObject().MemoryStream)
                {
                    Serializer.SerializeToStream(value, serializedStream);

                    if (serializedStream.Length > Settings.MinValueLengthForCompression)
                    {
                        // Stream is too long, we should compress it.
                        using (var compressedStream = MemoryStreamPool.GetObject().MemoryStream)
                        {
                            using (var compressionStream = Compressor.CreateCompressionStream(compressedStream))
                            {
                                serializedStream.Position = 0L;
                                serializedStream.CopyTo(compressionStream);
                            }
                            serializedValue = compressedStream.ToArray();
                            compressed = true;
                        }
                    }
                    else
                    {
                        // Stream is shorter than specified threshold, we can store it as it is.
                        serializedValue = serializedStream.ToArray();
                        compressed = false;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnSerializationFormat, ex, value?.ToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            var policy = (interval == TimeSpan.Zero)
                ? new SystemCacheItemPolicy { AbsoluteExpiration = utcExpiry }
                : new SystemCacheItemPolicy { SlidingExpiration = interval };

            if (parentKeys != null && parentKeys.Count > 0)
            {
                policy.ChangeMonitors.Add(_store.CreateCacheEntryChangeMonitor(parentKeys.Select(pk => SerializeCacheKey(partition, pk))));
            }

            var cacheValue = new CacheValue { Value = serializedValue, Compressed = compressed, UtcCreation = Clock.UtcNow };
            _store.Set(SerializeCacheKey(partition, key), cacheValue, policy);
        }

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected override long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            // We need to make a snapshot of the keys, since the cache might be used by other
            // processes. Therefore, we start projecting all keys.
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

            return keysArray.LongLength;
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected override bool ContainsInternal(string partition, string key)
        {
            var maybeCacheKey = SerializeCacheKey(partition, key);
            return _store.Contains(maybeCacheKey);
        }

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            // If partition has not been specified, then we use the GetCount method provided directly
            // by the MemoryCache.
            if (partition == null)
            {
                return _store.GetCount();
            }

            // Otherwise, we need to count items, which is surely slower. In fact, we also need to
            // deserialize the key in order to understand if the item belongs to the partition.
            return _store.Count(x => DeserializeCacheKey(x.Key).Partition == partition);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected override CacheResult<TVal> GetInternal<TVal>(string partition, string key)
        {
            var maybeCacheKey = SerializeCacheKey(partition, key);
            var maybeCacheValue = _store.Get(maybeCacheKey) as CacheValue;
            return DeserializeCacheValue<TVal>(maybeCacheValue);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected override CacheResult<ICacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            var maybeCacheKey = SerializeCacheKey(partition, key);
            var maybeCacheValue = _store.Get(maybeCacheKey) as CacheValue;
            return DeserializeCacheItem<TVal>(maybeCacheValue, partition, key);
        }

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected override IList<ICacheItem<TVal>> GetItemsInternal<TVal>(string partition)
        {
            // Pick only the items with the right type.
            var q = _store.Where(x => x.Value is CacheValue).Select(x => new
            {
                CacheKey = DeserializeCacheKey(x.Key),
                CacheValue = x.Value as CacheValue
            });

            // If partition has been specified, then we shall also filter by it.
            if (partition != null)
            {
                q = q.Where(x => x.CacheKey.Partition == partition);
            }

            // Project the items to proper KVLite cache items.
            return q.Select(x => DeserializeCacheItem<TVal>(x.CacheValue, x.CacheKey.Partition, x.CacheKey.Key).Value).ToArray();
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected override CacheResult<TVal> PeekInternal<TVal>(string partition, string key)
        {
            throw new NotSupportedException(ErrorMessages.CacheDoesNotAllowPeeking);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected override CacheResult<ICacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            throw new NotSupportedException(ErrorMessages.CacheDoesNotAllowPeeking);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="T:System.Object"/> as type parameter; that will work whether the required
        ///   value is a class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected override IList<ICacheItem<TVal>> PeekItemsInternal<TVal>(string partition)
        {
            throw new NotSupportedException(ErrorMessages.CacheDoesNotAllowPeeking);
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected override void RemoveInternal(string partition, string key)
        {
            var maybeCacheKey = SerializeCacheKey(partition, key);
            _store.Remove(maybeCacheKey);
        }

        #endregion ICache members

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(Settings.CacheName):
                    InitSystemMemoryCache();
                    break;
            }
        }

        private void InitSystemMemoryCache()
        {
            // If a memory cache was already instanced, and it was not the default, the dispose it
            // after applying the new initialization.
            if (_store != null && _store != SystemMemoryCache.Default)
            {
                _store.Dispose();
            }

            if (Settings.CacheName == MemoryCacheSettings.Default.CacheName)
            {
                // If the default cache name is used, then refer to the Default memory cache. It is
                // the safest and most efficient way to use that kind of cache.
                _store = SystemMemoryCache.Default;
                return;
            }

            // Otherwise, if a name has been specified, then we need to apply a proper configuration.
            // This way is more dangerous, because it is not easy to choose the right moment to
            // dispose the memory cache.
            _store = new SystemMemoryCache(Settings.CacheName, new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", Settings.MaxCacheSizeInMB.ToString() }
            });
        }

        #region Cache size estimation

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected override long GetCacheSizeInBytesInternal() => _store
            .Select(x => x.Value as CacheValue)
            .Where(x => x != null)
            .Sum(x => x.Value.LongLength);

        #endregion Cache size estimation

        #region Cache key handling

        [Serializable, DataContract]
        private struct CacheKey
        {
            [DataMember(Name = "p", Order = 0, EmitDefaultValue = false)]
            public string Partition { get; set; }

            [DataMember(Name = "k", Order = 1, EmitDefaultValue = false)]
            public string Key { get; set; }
        }

        [Serializable, DataContract]
        private sealed class CacheValue
        {
            [DataMember(Name = "vl", Order = 0, EmitDefaultValue = false)]
            public byte[] Value { get; set; }

            [DataMember(Name = "co", Order = 1, EmitDefaultValue = false)]
            public bool Compressed { get; set; }

            [DataMember(Name = "cr", Order = 2, EmitDefaultValue = false)]
            public DateTime UtcCreation { get; set; }
        }

        private static string SerializeCacheKey(string partition, string key)
        {
            var partitionLength = partition.Length;
            return $"{partitionLength}${partition}${key}";
        }

        private static CacheKey DeserializeCacheKey(string cacheKey)
        {
            var partitionLengthEnd = cacheKey.IndexOf('$');
            var partitionLengthPrefix = cacheKey.Substring(0, partitionLengthEnd);
            var partitionLength = int.Parse(partitionLengthPrefix);
            return new CacheKey
            {
                Partition = cacheKey.Substring(partitionLengthEnd + 1, partitionLength),
                Key = cacheKey.Substring(partitionLengthEnd + partitionLength + 2)
            };
        }

        private TVal UnsafeDeserializeCacheValue<TVal>(CacheValue cacheValue)
        {
            using (var memoryStream = new MemoryStream(cacheValue.Value))
            {
                if (!cacheValue.Compressed)
                {
                    // Handle uncompressed value.
                    return Serializer.DeserializeFromStream<TVal>(memoryStream);
                }
                using (var decompressionStream = Compressor.CreateDecompressionStream(memoryStream))
                {
                    // Handle compressed value.
                    return Serializer.DeserializeFromStream<TVal>(decompressionStream);
                }
            }
        }

        private CacheResult<TVal> DeserializeCacheValue<TVal>(CacheValue cacheValue)
        {
            if (cacheValue == null || cacheValue.Value == null)
            {
                // Nothing to deserialize, return None.
                return default(CacheResult<TVal>);
            }
            try
            {
                return UnsafeDeserializeCacheValue<TVal>(cacheValue);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex);
                return default(CacheResult<TVal>);
            }
        }

        private CacheResult<ICacheItem<TVal>> DeserializeCacheItem<TVal>(CacheValue cacheValue, string partition, string key)
        {
            if (cacheValue == null || cacheValue.Value == null)
            {
                // Nothing to deserialize, return None.
                return default(CacheResult<ICacheItem<TVal>>);
            }
            try
            {
                // Generate the KVLite cache item and return it. Many properties available in the
                // KVLite cache items cannot be filled due to missing information.
                return new CacheItem<TVal>
                {
                    Partition = partition,
                    Key = key,
                    Value = UnsafeDeserializeCacheValue<TVal>(cacheValue),
                    UtcCreation = cacheValue.UtcCreation
                };
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex);
                return default(CacheResult<ICacheItem<TVal>>);
            }
        }

        #endregion Cache key handling
    }
}
