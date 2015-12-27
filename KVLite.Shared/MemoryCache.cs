// File name: MemoryCache.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using SystemCacheItemPolicy = System.Runtime.Caching.CacheItemPolicy;
using SystemMemoryCache = System.Runtime.Caching.MemoryCache;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An in-memory cache based on the .NET <see cref="SystemMemoryCache"/>.
    /// </summary>
    public sealed class MemoryCache : AbstractCache<MemoryCacheSettings>, IDisposable
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
        SystemMemoryCache _store;

        /// <summary>
        ///   Whether this cache has been disposed or not. When a cache has been disposed, no more
        ///   operations are allowed on it.
        /// </summary>
        bool _disposed;

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

        #region FormattableObject members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:Finsa.CodeServices.Common.FormattableObject.ToString"/>.
        /// </summary>
        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return KeyValuePair.Create("SystemMemoryCacheName", _store.Name);
        }

        #endregion FormattableObject members

        #region IDisposable members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <remarks>
        ///   When called on a cache using <see cref="SystemMemoryCache.Default"/> as store, this
        ///   method does nothing. In fact, it is not safe to dispose the default memory cache instance.
        /// </remarks>
        public void Dispose()
        {
            if (_disposed)
            {
                // Nothing to do, object has been disposed.
                return;
            }

            if (_store != null && _store != SystemMemoryCache.Default)
            {
                _store.Dispose();
                _store = null;
            }

            _disposed = true;
        }

        #endregion IDisposable members

        #region ICache members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>The clock used by the cache.</value>
        /// <remarks>
        ///   Since <see cref="SystemMemoryCache"/> does not allow clock customisation, then this
        ///   property defaults to <see cref="T:Finsa.CodeServices.Clock.SystemClock"/>.
        /// </remarks>
        public override IClock Clock { get; } = new SystemClock();

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        /// <remarks>
        ///   Since compression is not used inside this kind of cache, then this property defaults
        ///   to <see cref="NoOpCompressor"/>.
        /// </remarks>
        public override ICompressor Compressor { get; } = new NoOpCompressor();

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to what
        ///   <see cref="M:Common.Logging.LogManager.GetLogger(System.Type)"/> returns.
        /// </remarks>
        public override ILog Log { get; }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>. Therefore,
        ///   if you do not specify another serializer, make sure that your objects are serializable
        ///   (in most cases, simply use the <see cref="SerializableAttribute"/> and expose fields
        ///   as public properties).
        /// </remarks>
        public override ISerializer Serializer { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        /// <value>The available settings for the cache.</value>
        public override MemoryCacheSettings Settings { get; }

        /// <summary>
        ///   True if the Peek methods are implemented, false otherwise.
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
        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

            var policy = (interval == TimeSpan.Zero)
                ? new SystemCacheItemPolicy { AbsoluteExpiration = utcExpiry }
                : new SystemCacheItemPolicy { SlidingExpiration = interval };

            _store.Set(SerializeToCacheKey(partition, key), value, policy);
        }

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        protected override void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

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

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected override bool ContainsInternal(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

            var maybeCacheKey = SerializeToCacheKey(partition, key);
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
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

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

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

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

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

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

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

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

            // Project the items to proper KVLite cache items. Many properties available in the
            // KVLite cache items cannot be filled due to missing information.
            return q.Select(x => new CacheItem<TVal>
            {
                Partition = x.CacheKey.Partition,
                Key = x.CacheKey.Key,
                Value = x.Value
            }).ToArray();
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
        protected override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
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
        protected override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating
        ///   expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="T:System.Object"/> as type parameter; that will work whether the required
        ///   value is a class or not.
        /// </remarks>
        protected override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException(ErrorMessages.MemoryCacheDoesNotAllowPeeking);
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected override void RemoveInternal(string partition, string key)
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

            var maybeCacheKey = SerializeToCacheKey(partition, key);
            _store.Remove(maybeCacheKey);
        }

        #endregion ICache members

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
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

            // If a memory cache was already instanced, and it was not the default, the dispose it
            // after applying the new initialization.
            if (_store != null && _store != SystemMemoryCache.Default)
            {
                _store.Dispose();
            }

            if (Settings.CacheName == MemoryCacheConfiguration.Instance.DefaultCacheName)
            {
                // If the default cache name is used, then refer to the Default memory cache. It is
                // the safest and most efficient way to use that kind of cache.
                _store = SystemMemoryCache.Default;
                return;
            }

            // Otherwise, if a name has been specified, then we need to apply a proper
            // configuration. This way is more dangerous, because it is not easy to choose the right
            // moment to dispose the memory cache.
            _store = new SystemMemoryCache(Settings.CacheName, new NameValueCollection
            {
                { "CacheMemoryLimitMegabytes", Settings.MaxCacheSizeInMB.ToString() }
            });
        }

        #region Cache size estimation

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        [Pure]
        public long CacheSizeInKB()
        {
            // Preconditions
            RaiseObjectDisposedException.If(_disposed, ErrorMessages.MemoryCacheHasBeenDisposed);

            var serializer = new BinarySerializer();
            var cacheItems = _store.ToArray();
            using (var stream = serializer.SerializeToStream(cacheItems))
            {
                return stream.Length / 1024L;
            }
        }

        #endregion Cache size estimation

        #region Cache key handling

        [Serializable, DataContract]
        struct CacheKey
        {
            [DataMember(Name = "p", Order = 0, EmitDefaultValue = false)]
            public string Partition { get; set; }

            [DataMember(Name = "k", Order = 1, EmitDefaultValue = false)]
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
