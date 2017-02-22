// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using CodeProject.ObjectPool.Specialized;
using PommaLabs.CodeServices.Caching.Core;
using PommaLabs.CodeServices.Common;
using PommaLabs.CodeServices.Common.Logging;
using PommaLabs.CodeServices.Common.Threading.Tasks;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.CodeServices.Caching
{
    /// <summary>
    ///   Abstract class which should make it easier to implement a new kind of cache.
    /// </summary>
    /// <typeparam name="TSettings">The type of the cache settings.</typeparam>
    public abstract class AbstractCache<TSettings> : FormattableObject, ICache<TSettings>, IAsyncCache<TSettings>
#if !NET40
        , Microsoft.Extensions.Caching.Distributed.IDistributedCache
#endif
        where TSettings : AbstractCacheSettings<TSettings>
    {
        #region Abstract members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public abstract IClock Clock { get; }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="GZipCompressor"/>.
        /// </remarks>
        public abstract ICompressor Compressor { get; }

        /// <summary>
        ///   The maximum number of parent keys each item can have.
        /// </summary>
        public abstract int MaxParentKeyCountPerItem { get; }

        /// <summary>
        ///   The pool used to retrieve <see cref="MemoryStream"/> instances.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="MemoryStreamPool.Instance"/>.
        /// </remarks>
        public abstract IMemoryStreamPool MemoryStreamPool { get; }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>.
        /// </remarks>
        public abstract ISerializer Serializer { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        /// <value>The available settings for the cache.</value>
        public abstract TSettings Settings { get; }

        /// <summary>
        ///   <c>true</c> if the Peek methods are implemented, <c>false</c> otherwise.
        /// </summary>
        public abstract bool CanPeek { get; }

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        protected ILog Log { get; } = LogProvider.GetCurrentClassLogger();

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected abstract long GetCacheSizeInBytesInternal();

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected virtual Task<long> GetCacheSizeInBytesAsyncInternal(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<long>(cancellationToken);
            }
            return TaskHelper.ResultTask(GetCacheSizeInBytesInternal());
        }

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
        protected abstract void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys);

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
        /// <param name="cancellationToken">An optional cancellation token.</param>
        protected virtual Task AddAsyncInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<object>(cancellationToken);
            }
            AddInternal(partition, key, value, utcExpiry, interval, parentKeys);
            return TaskHelper.CompletedTask;
        }

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected abstract long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate);

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected virtual Task<long> ClearAsyncInternal(string partition, CacheReadMode cacheReadMode, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<long>(cancellationToken);
            }
            var result = ClearInternal(partition, cacheReadMode);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected abstract bool ContainsInternal(string partition, string key);

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected virtual Task<bool> ContainsAsyncInternal(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<bool>(cancellationToken);
            }
            var result = ContainsInternal(partition, key);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected abstract long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate);

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected virtual Task<long> CountAsyncInternal(string partition, CacheReadMode cacheReadMode, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<long>(cancellationToken);
            }
            var result = CountInternal(partition, cacheReadMode);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected abstract Option<TVal> GetInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected virtual Task<Option<TVal>> GetAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<Option<TVal>>(cancellationToken);
            }
            var result = GetInternal<TVal>(partition, key);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected abstract Option<ICacheItem<TVal>> GetItemInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected virtual Task<Option<ICacheItem<TVal>>> GetItemAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<Option<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = GetItemInternal<TVal>(partition, key);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected abstract IList<ICacheItem<TVal>> GetItemsInternal<TVal>(string partition);

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected virtual Task<IList<ICacheItem<TVal>>> GetItemsAsyncInternal<TVal>(string partition, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<IList<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = GetItemsInternal<TVal>(partition);
            return TaskHelper.ResultTask(result);
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
        protected abstract Option<TVal> PeekInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected virtual Task<Option<TVal>> PeekAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<Option<TVal>>(cancellationToken);
            }
            var result = PeekInternal<TVal>(partition, key);
            return TaskHelper.ResultTask(result);
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
        protected abstract Option<ICacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key);

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected virtual Task<Option<ICacheItem<TVal>>> PeekItemAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<Option<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = PeekItemInternal<TVal>(partition, key);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected abstract IList<ICacheItem<TVal>> PeekItemsInternal<TVal>(string partition);

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        protected virtual Task<IList<ICacheItem<TVal>>> PeekItemsAsyncInternal<TVal>(string partition, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<IList<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = PeekItemsInternal<TVal>(partition);
            return TaskHelper.ResultTask(result);
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected abstract void RemoveInternal(string partition, string key);

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        protected virtual Task RemoveAsyncInternal(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return TaskHelper.CanceledTask<object>(cancellationToken);
            }
            RemoveInternal(partition, key);
            return TaskHelper.CompletedTask;
        }

        #endregion Abstract members

        #region IDisposable members

        /// <summary>
        ///   Whether this cache has been disposed or not. When a cache has been disposed, no more
        ///   operations are allowed on it.
        /// </summary>
        public bool Disposed { get; private set; }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (Disposed)
            {
                // Nothing to do, cache has been disposed.
                return;
            }

            Dispose(true);

            // Mark this class as disposed.
            Disposed = true;

            // Use SupressFinalize in case a subclass of this type implements a finalizer.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if it is a managed dispose, false otherwise.</param>
        protected abstract void Dispose(bool disposing);

        #endregion IDisposable members

        #region IEssentialCache members

        /// <summary>
        ///   The last error "swallowed" by the cache. All KVLite caches, by definition, try to
        ///   swallow as much exceptions as possible, because a failure in the cache should never
        ///   harm the main application. This is an important rule.
        ///
        ///   This property might be used to expose the last error occurred while processing cache
        ///   items. If no error has occurred, this property will simply be null.
        ///
        ///   Every error is carefully logged using the provided
        ///   <see cref="P:PommaLabs.KVLite.ICache.Log"/>, so no information is lost when the cache
        ///   swallows the exception.
        /// </summary>
        public Exception LastError { get; set; }

        #endregion IEssentialCache members

        #region ICache members

        /// <summary>
        ///   The settings available for the cache.
        /// </summary>
        /// <value>The settings available for the cache.</value>
        ICacheSettings ICache.Settings => Settings;

        /// <summary>
        ///   Gets the value with the specified partition and key.
        /// </summary>
        /// <value>The value with the specified partition and key.</value>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified partition and key.</returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <remarks>
        ///   This method, differently from other readers (like
        ///   <see cref="Get{TVal}(string,string)"/> or <see cref="Peek{TVal}(string,string)"/>),
        ///   does not have a typed return object, because indexers cannot be generic. Therefore, we
        ///   have to return a simple <see cref="object"/>.
        /// </remarks>
        public Option<object> this[string partition, string key]
        {
            get
            {
                // Preconditions
                Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
                Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
                Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

                try
                {
                    var result = GetInternal<object>(partition, key);

                    // Postconditions
                    Debug.Assert(Contains(partition, key) == result.HasValue);
                    return result;
                }
                catch (Exception ex)
                {
                    LastError = ex;
                    Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                    return Option.None<object>();
                }
            }
        }

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <returns>An estimate of cache size in bytes.</returns>
        public long GetCacheSizeInBytes()
        {
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = GetCacheSizeInBytesInternal();

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadAll), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddSliding<TVal>(string partition, string key, TVal value, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "static" value with given partition and key. Value will last as much as
        ///   specified in <see cref="IEssentialCacheSettings.StaticIntervalInDays"/> and, if
        ///   accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddStatic<TVal>(string partition, string key, TVal value, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimed<TVal>(string partition, string key, TVal value, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                AddInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last for the specified
        ///   lifetime and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public void AddTimed<TVal>(string partition, string key, TVal value, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = ClearInternal(null);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count() == 0);
                Debug.Assert(LongCount() == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear(string partition)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = ClearInternal(partition);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count(partition) == 0);
                Debug.Assert(LongCount(partition) == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnClearPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public bool Contains(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                return ContainsInternal(partition, key);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return false;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public int Count()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = Convert.ToInt32(CountInternal(null));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public int Count(string partition)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = Convert.ToInt32(CountInternal(partition));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public long LongCount()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = CountInternal(null);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public long LongCount(string partition)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = CountInternal(partition);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        public Option<TVal> Get<TVal>(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        public Option<ICacheItem<TVal>> GetItem<TVal>(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = GetItemInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<ICacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will be
        ///   increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        public IList<ICacheItem<TVal>> GetItems<TVal>()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = GetItemsInternal<TVal>(null);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count());
                Debug.Assert(result.LongCount() == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        public IList<ICacheItem<TVal>> GetItems<TVal>(string partition)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = GetItemsInternal<TVal>(partition);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count(partition));
                Debug.Assert(result.LongCount() == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "sliding" value with given partition and key.
        ///   Value will last as much as specified in given interval and, if accessed before expiry,
        ///   its lifetime will be extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddSliding<TVal>(string partition, string key, Func<TVal> valueGetter, TimeSpan interval, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given partition and key.
        ///   Value will last as much as specified in
        ///   <see cref="P:PommaLabs.KVLite.Core.AbstractCacheSettings.StaticIntervalInDays"/> and,
        ///   if accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddStatic<TVal>(string partition, string key, Func<TVal> valueGetter, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last until the specified time and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimed<TVal>(string partition, string key, Func<TVal> valueGetter, DateTime utcExpiry, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last for the specified lifetime and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="MaxParentKeyCountPerItem"/> to understand how many parent keys each item may have.
        /// </exception>
        public TVal GetOrAddTimed<TVal>(string partition, string key, Func<TVal> valueGetter, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = GetInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = valueGetter.Invoke();
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                AddInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<TVal> Peek<TVal>(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
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
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public Option<ICacheItem<TVal>> PeekItem<TVal>(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemInternal<TVal>(partition, key);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<ICacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public IList<ICacheItem<TVal>> PeekItems<TVal>()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemsInternal<TVal>(null);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count());
                Debug.Assert(result.LongCount() == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the <see cref="CanPeek"/> property).
        /// </exception>
        public IList<ICacheItem<TVal>> PeekItems<TVal>(string partition)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = PeekItemsInternal<TVal>(partition);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count(partition));
                Debug.Assert(result.LongCount() == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        public void Remove(string partition, string key)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                RemoveInternal(partition, key);

                // Postconditions
                Debug.Assert(!Contains(partition, key));
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        #endregion ICache members

        #region IAsyncCache members

        /// <summary>
        ///   The settings available for the async cache.
        /// </summary>
        /// <value>The settings available for the async cache.</value>
        IAsyncCacheSettings IAsyncCache.Settings => Settings;

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>An estimate of cache size in bytes.</returns>
        public async Task<long> GetCacheSizeInBytesAsync(CancellationToken cancellationToken)
        {
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = await GetCacheSizeInBytesAsyncInternal(cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadAll), ex);
                return 0L;
            }
        }

        Task<Option<object>> IAsyncCache.this[string partition, string key]
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task AddSlidingAsync<TVal>(string partition, string key, TVal value, TimeSpan interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "static" value with given partition and key. Value will last as much as
        ///   specified in <see cref="IEssentialCacheSettings.StaticIntervalInDays"/> and, if
        ///   accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task AddStaticAsync<TVal>(string partition, string key, TVal value, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, DateTime utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last for the specified
        ///   lifetime and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, TimeSpan lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all stored items.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        public async Task<long> ClearAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = await ClearAsyncInternal(null, CacheReadMode.IgnoreExpiryDate, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count() == 0);
                Debug.Assert(LongCount() == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its items.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="partition"/> is null.</exception>
        public async Task<long> ClearAsync(string partition, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = await ClearAsyncInternal(partition, CacheReadMode.IgnoreExpiryDate, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result >= 0L);
                Debug.Assert(Count(partition) == 0);
                Debug.Assert(LongCount(partition) == 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnClearPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items stored in the cache.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public async Task<int> CountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = Convert.ToInt32(await CountAsyncInternal(null, CacheReadMode.ConsiderExpiryDate, cancellationToken).ConfigureAwait(false));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items stored in given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="partition"/> is null.</exception>
        public async Task<int> CountAsync(string partition, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = Convert.ToInt32(await CountAsyncInternal(partition, CacheReadMode.ConsiderExpiryDate, cancellationToken).ConfigureAwait(false));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items stored in the cache.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public async Task<long> LongCountAsync(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = await CountAsyncInternal(null, CacheReadMode.ConsiderExpiryDate, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items stored in given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in given partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="partition"/> is null.</exception>
        public async Task<long> LongCountAsync(string partition, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = await CountAsyncInternal(partition, CacheReadMode.ConsiderExpiryDate, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Determines whether this cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>Whether this cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        public async Task<bool> ContainsAsync(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                return await ContainsAsyncInternal(partition, key, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return false;
            }
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <returns>The value with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        public async Task<Option<TVal>> GetAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = await GetAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <returns>The cache item with specified partition and key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        public async Task<Option<ICacheItem<TVal>>> GetItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                var result = await GetItemAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<ICacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will be
        ///   increased by corresponding interval.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        public async Task<IList<ICacheItem<TVal>>> GetItemsAsync<TVal>(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                var result = await GetItemsAsyncInternal<TVal>(null, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count());
                Debug.Assert(result.LongCount() == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items in given partition.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="partition"/> is null.</exception>
        public async Task<IList<ICacheItem<TVal>>> GetItemsAsync<TVal>(string partition, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);

            try
            {
                var result = await GetItemsAsyncInternal<TVal>(partition, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count(partition));
                Debug.Assert(result.LongCount() == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "sliding" value with given partition and key.
        ///   Value will last as much as specified in given interval and, if accessed before expiry,
        ///   its lifetime will be extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/>, <paramref name="key"/> or <paramref name="valueGetter"/>
        ///   are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task<TVal> GetOrAddSlidingAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, TimeSpan interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = await GetAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given partition and key.
        ///   Value will last as much as specified in
        ///   <see cref="IEssentialCacheSettings.StaticIntervalInDays"/> and, if accessed before
        ///   expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/>, <paramref name="key"/> or <paramref name="valueGetter"/>
        ///   are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task<TVal> GetOrAddStaticAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = await GetAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last until the specified time and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/>, <paramref name="key"/> or <paramref name="valueGetter"/>
        ///   are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, DateTime utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = await GetAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last for the specified lifetime and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="valueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>
        ///   The value found in the cache or the one returned by <paramref name="valueGetter"/>, in
        ///   case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/>, <paramref name="key"/> or <paramref name="valueGetter"/>
        ///   are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, TimeSpan lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(valueGetter, nameof(valueGetter), ErrorMessages.NullValueGetter);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = await GetAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow.Add(lifetime), TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public async Task<Option<TVal>> PeekAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = await PeekAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<TVal>();
            }
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public async Task<Option<ICacheItem<TVal>>> PeekItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = await PeekItemAsyncInternal<TVal>(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(Contains(partition, key) == result.HasValue);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
                return Option.None<ICacheItem<TVal>>();
            }
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public async Task<IList<ICacheItem<TVal>>> PeekItemsAsync<TVal>(CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = await PeekItemsAsyncInternal<TVal>(null, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count());
                Debug.Assert(result.LongCount() == LongCount());
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="partition"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public async Task<IList<ICacheItem<TVal>>> PeekItemsAsync<TVal>(string partition, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.NotSupportedException.IfNot(CanPeek, ErrorMessages.CacheDoesNotAllowPeeking);

            try
            {
                var result = await PeekItemsAsyncInternal<TVal>(partition, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(result != null);
                Debug.Assert(result.Count == Count(partition));
                Debug.Assert(result.LongCount() == LongCount(partition));
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnReadPartition, partition), ex);
                return new ICacheItem<TVal>[0];
            }
        }

        /// <summary>
        ///   Removes the item with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        public async Task RemoveAsync(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);

            try
            {
                await RemoveAsyncInternal(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key));
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }
        }

        #endregion IAsyncCache members

        #region IDistributedCache members

#if !NET40

        byte[] Microsoft.Extensions.Caching.Distributed.IDistributedCache.Get(string key) => Get<byte[]>(Settings.DefaultPartition, key).ValueOrDefault();

        async Task<byte[]> Microsoft.Extensions.Caching.Distributed.IDistributedCache.GetAsync(string key) => (await GetAsync<byte[]>(Settings.DefaultPartition, key).ConfigureAwait(false)).ValueOrDefault();

        void Microsoft.Extensions.Caching.Distributed.IDistributedCache.Set(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue && (options.AbsoluteExpiration.HasValue || options.AbsoluteExpirationRelativeToNow.HasValue))
            {
                throw new NotImplementedException(ErrorMessages.CacheDoesNotAllowSlidingAndAbsolute);
            }
            if (options.SlidingExpiration.HasValue)
            {
                AddSliding(Settings.DefaultPartition, key, value, options.SlidingExpiration.Value);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                AddTimed(Settings.DefaultPartition, key, value, options.AbsoluteExpiration.Value.ToUniversalTime().DateTime);
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                AddTimed(Settings.DefaultPartition, key, value, options.AbsoluteExpirationRelativeToNow.Value);
            }
        }

        async Task Microsoft.Extensions.Caching.Distributed.IDistributedCache.SetAsync(string key, byte[] value, Microsoft.Extensions.Caching.Distributed.DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue && (options.AbsoluteExpiration.HasValue || options.AbsoluteExpirationRelativeToNow.HasValue))
            {
                throw new NotImplementedException(ErrorMessages.CacheDoesNotAllowSlidingAndAbsolute);
            }
            if (options.SlidingExpiration.HasValue)
            {
                await AddSlidingAsync(Settings.DefaultPartition, key, value, options.SlidingExpiration.Value).ConfigureAwait(false);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                await AddTimedAsync(Settings.DefaultPartition, key, value, options.AbsoluteExpiration.Value.ToUniversalTime().DateTime).ConfigureAwait(false);
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                await AddTimedAsync(Settings.DefaultPartition, key, value, options.AbsoluteExpirationRelativeToNow.Value).ConfigureAwait(false);
            }
        }

        void Microsoft.Extensions.Caching.Distributed.IDistributedCache.Refresh(string key) => Get<byte[]>(Settings.DefaultPartition, key);

        Task Microsoft.Extensions.Caching.Distributed.IDistributedCache.RefreshAsync(string key) => GetAsync<byte[]>(Settings.DefaultPartition, key);

        void Microsoft.Extensions.Caching.Distributed.IDistributedCache.Remove(string key) => Remove(Settings.DefaultPartition, key);

        Task Microsoft.Extensions.Caching.Distributed.IDistributedCache.RemoveAsync(string key) => RemoveAsync(Settings.DefaultPartition, key);

#endif

        #endregion IDistributedCache members
    }
}
