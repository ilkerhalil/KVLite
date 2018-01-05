// File name: MemoryCache.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
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

using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Logging;
using PommaLabs.KVLite.Resources;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using MicrosoftMemoryCache = Microsoft.Extensions.Caching.Memory.MemoryCache;
using MicrosoftMemoryCacheOptions = Microsoft.Extensions.Caching.Memory.MemoryCacheOptions;

namespace PommaLabs.KVLite.Memory
{
    /// <summary>
    ///   An in-memory cache based on the same logic used for RDMBS-specific drivers.
    /// </summary>
    public sealed class MemoryCache : AbstractCache<MemoryCache, MemoryCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        public static MemoryCache DefaultInstance { get; } = new MemoryCache(new MemoryCacheSettings());

        #endregion Default Instance

        #region Construction

        /// <summary>
        ///   The Microsoft memory cache used as backend.
        /// </summary>
        private MicrosoftMemoryCache _store;

        /// <summary>
        ///   Helper map used to enumerate entries, something which has not been implemented by
        ///   Microsoft memory cache.
        /// </summary>
        private ConcurrentDictionary<MemoryCacheKey, MemoryCacheValue> _helperMap;

        /// <summary>
        ///   Initializes a new instance of the <see cref="MemoryCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="clock">The clock.</param>
        public MemoryCache(MemoryCacheSettings settings, ISerializer serializer = null, ICompressor compressor = null, IClock clock = null)
        {
            Settings = settings;
            Compressor = compressor ?? DeflateCompressor.Instance;
            Serializer = serializer ?? JsonSerializer.Instance;
            Clock = clock ?? SystemClock.Instance;

            InitMicrosoftMemoryCache();
        }

        private void InitMicrosoftMemoryCache()
        {
            // If a memory cache was already instanced, then dispose it before applying the new initialization.
            _store?.Dispose();
            _store = new MicrosoftMemoryCache(new OptionsWrapper<MicrosoftMemoryCacheOptions>(new MicrosoftMemoryCacheOptions
            {
                Clock = Clock,
#if !NET45
                SizeLimit = Settings.MaxCacheSizeInMB * 1024 * 1024
#endif
            }));

            // Clear the helper map, since we are replacing the memory cache.
            _helperMap?.Clear();
            _helperMap = new ConcurrentDictionary<MemoryCacheKey, MemoryCacheValue>();
        }

        #endregion Construction

        #region FormattableObject members

        /// <summary>
        ///   Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString() => $"MicrosoftMemoryCache: {_store.Count} entries";

        #endregion FormattableObject members

        #region IDisposable members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if it is a managed dispose, false otherwise.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                // Nothing to do, we can handle only managed Dispose calls.
                return;
            }

            _store?.Dispose();
            _store = null;

            _helperMap?.Clear();
            _helperMap = null;
        }

        #endregion IDisposable members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
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
        ///   The maximum depth the tree generated by parent keys can have. The .NET memory cache
        ///   supports an unlimited depth for parent key nesting.
        /// </summary>
        public override int MaxParentKeyTreeDepth { get; } = int.MaxValue;

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
        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTimeOffset utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.AddItem, partition, key, Settings.CacheName, utcExpiry, interval);
            }

            byte[] serializedValue;
            bool compressed;
            try
            {
                using (var serializedStream = new PooledMemoryStream())
                {
                    Serializer.SerializeToStream(value, serializedStream);

                    if (serializedStream.Length > Settings.MinValueLengthForCompression)
                    {
                        // Stream is too long, we should compress it.
                        using (var compressedStream = new PooledMemoryStream())
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
                Log.ErrorException(ErrorMessages.InternalErrorOnSerialization, ex, value);
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            var cacheKey = new MemoryCacheKey(partition, key);
            var cacheValue = new MemoryCacheValue
            {
                CacheKey = cacheKey,
                Value = serializedValue,
                Compressed = compressed,
                UtcCreation = Clock.UtcNow
            };

            var cacheEntryOptions = new MemoryCacheEntryOptions
            {
#if !NET45
                Size = cacheValue.Value.LongLength
#endif
            };

            if (interval == TimeSpan.Zero)
            {
                cacheEntryOptions.AbsoluteExpiration = utcExpiry;
            }
            else
            {
                cacheEntryOptions.SlidingExpiration = interval;
            }

            if (parentKeys != null && parentKeys.Count > 0)
            {
                cacheValue.ParentKeys = new HashSet<MemoryCacheKey>(parentKeys.Select(pk => new MemoryCacheKey(partition, pk)));
            }

            _store.Set(cacheKey, cacheValue, cacheEntryOptions);
            _helperMap.AddOrUpdate(cacheKey, cacheValue, (k, o) => cacheValue);
        }

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected override long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            var q = _helperMap.Keys.AsEnumerable();

            if (partition != null)
            {
                q = q.Where(k => k.Partition == partition);
            }

            var cacheKeys = q.ToArray();

            foreach (var cacheKey in cacheKeys)
            {
                _helperMap.TryRemove(cacheKey, out var _);
                _store.Remove(cacheKey);
            }

            return cacheKeys.LongLength;
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
            var cacheKey = new MemoryCacheKey(partition, key);
            return _helperMap.ContainsKey(cacheKey) && _store.TryGetValue(cacheKey, out var _);
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
            var q = _helperMap.Keys.Cast<MemoryCacheKey>();
            if (partition != null)
            {
                q = q.Where(k => k.Partition == partition);
            }
            return q.Count(k => _store.TryGetValue(k, out var _));
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
            var cacheKey = new MemoryCacheKey(partition, key);
            if (_helperMap.ContainsKey(cacheKey) && _store.TryGetValue(cacheKey, out var tmp))
            {
                return DeserializeCacheValue<TVal>(tmp as MemoryCacheValue, partition, key);
            }
            return default;
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
            var cacheKey = new MemoryCacheKey(partition, key);
            if (_helperMap.ContainsKey(cacheKey) && _store.TryGetValue(cacheKey, out var tmp))
            {
                return DeserializeCacheItem<TVal>(tmp as MemoryCacheValue, partition, key);
            }
            return default;
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
            var q = _helperMap.Keys.Cast<MemoryCacheKey>();

            if (partition != null)
            {
                q = q.Where(k => k.Partition == partition);
            }

            var cacheKeys = q.ToArray();

            var items = new List<ICacheItem<TVal>>();
            foreach (var cacheKey in cacheKeys)
            {
                if (_store.TryGetValue(cacheKey, out var tmp))
                {
                    items.Add(DeserializeCacheItem<TVal>(tmp as MemoryCacheValue, cacheKey.Partition, cacheKey.Key).Value);
                }
            }

            return items;
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
            throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));
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
            throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));
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
            throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected override void RemoveInternal(string partition, string key)
        {
            RemoveInternal(new MemoryCacheKey(partition, key));
        }

        #region Cache size estimation

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected override long GetCacheSizeInBytesInternal() => _helperMap.Values
            .Cast<MemoryCacheValue>()
            .Where(x => x != null && x.Value != null)
            .Sum(x => x.Value.LongLength);

        #endregion Cache size estimation

        #region Cache entry handling

        private void RemoveInternal(MemoryCacheKey cacheKey)
        {
            var childCacheValues = _helperMap.Values
                .Where(v => v.ParentKeys.Contains(cacheKey));

            foreach (var childCacheValue in childCacheValues)
            {
                RemoveInternal(childCacheValue.CacheKey);
            }

            _helperMap.TryRemove(cacheKey, out var _);
            _store.Remove(cacheKey);
        }

        private TVal UnsafeDeserializeCacheValue<TVal>(MemoryCacheValue cacheValue)
        {
            var buffer = cacheValue.Value;
            using (var memoryStream = new PooledMemoryStream(buffer))
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

        private CacheResult<TVal> DeserializeCacheValue<TVal>(MemoryCacheValue cacheValue, string partition, string key)
        {
            if (cacheValue == null || cacheValue.Value == null)
            {
                // Nothing to deserialize, return None.
                return default;
            }
            try
            {
                return UnsafeDeserializeCacheValue<TVal>(cacheValue);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex, partition, key, Settings.CacheName);
                return default;
            }
        }

        private CacheResult<ICacheItem<TVal>> DeserializeCacheItem<TVal>(MemoryCacheValue cacheValue, string partition, string key)
        {
            if (cacheValue == null || cacheValue.Value == null)
            {
                // Nothing to deserialize, return None.
                return default;
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
                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex, partition, key, Settings.CacheName);
                return default;
            }
        }

        #endregion Cache entry handling
    }
}
