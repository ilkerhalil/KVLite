// File name: AbstractAsyncCache.cs
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

using PommaLabs.KVLite.Logging;
using PommaLabs.KVLite.Resources;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    public abstract partial class AbstractCache<TCache, TSettings> : IAsyncCache<TSettings>
    {
        #region Abstract and virtual members

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
                return CanceledTask<long>(cancellationToken);
            }
            return Task.FromResult(GetCacheSizeInBytesInternal());
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
        /// <param name="cancellationToken">An optional cancellation token.</param>
        protected virtual Task AddAsyncInternal<TVal>(string partition, string key, TVal value, DateTimeOffset utcExpiry, TimeSpan interval, IList<string> parentKeys, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<object>(cancellationToken);
            }
            AddInternal(partition, key, value, utcExpiry, interval, parentKeys);
            return Task.FromResult(0);
        }

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
                return CanceledTask<long>(cancellationToken);
            }
            var result = ClearInternal(partition, cacheReadMode);
            return Task.FromResult(result);
        }

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
                return CanceledTask<bool>(cancellationToken);
            }
            var result = ContainsInternal(partition, key);
            return Task.FromResult(result);
        }

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
                return CanceledTask<long>(cancellationToken);
            }
            var result = CountInternal(partition, cacheReadMode);
            return Task.FromResult(result);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected virtual Task<CacheResult<TVal>> GetAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<CacheResult<TVal>>(cancellationToken);
            }
            var result = GetInternal<TVal>(partition, key);
            return Task.FromResult(result);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected virtual Task<CacheResult<ICacheItem<TVal>>> GetItemAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<CacheResult<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = GetItemInternal<TVal>(partition, key);
            return Task.FromResult(result);
        }

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
                return CanceledTask<IList<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = GetItemsInternal<TVal>(partition);
            return Task.FromResult(result);
        }

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
        protected virtual Task<CacheResult<TVal>> PeekAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<CacheResult<TVal>>(cancellationToken);
            }
            var result = PeekInternal<TVal>(partition, key);
            return Task.FromResult(result);
        }

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
        protected virtual Task<CacheResult<ICacheItem<TVal>>> PeekItemAsyncInternal<TVal>(string partition, string key, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return CanceledTask<CacheResult<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = PeekItemInternal<TVal>(partition, key);
            return Task.FromResult(result);
        }

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
                return CanceledTask<IList<ICacheItem<TVal>>>(cancellationToken);
            }
            var result = PeekItemsInternal<TVal>(partition);
            return Task.FromResult(result);
        }

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
                return CanceledTask<object>(cancellationToken);
            }
            RemoveInternal(partition, key);
            return Task.FromResult(0);
        }

        #endregion Abstract and virtual members

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
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex, Settings.CacheName);
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
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/> or <paramref name="key"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public async Task AddSlidingAsync<TVal>(string partition, string key, TVal value, TimeSpan interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
            }
        }

        /// <summary>
        ///   Adds a "static" value with given partition and key. Value will last as much as
        ///   specified in <see cref="IEssentialCacheSettings.StaticInterval"/> and, if accessed
        ///   before expiry, its lifetime will be extended by that interval.
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
        public async Task AddStaticAsync<TVal>(string partition, string key, TVal value, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
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
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, DateTimeOffset utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
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
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, TimeSpan lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + lifetime, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
            }
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all stored items.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        public async Task<long> ClearAsync(CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnClearAll, ex, Settings.CacheName);
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
        public async Task<long> ClearAsync(string partition, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnClearPartition, ex, Settings.CacheName, partition);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items stored in the cache.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public async Task<int> CountAsync(CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex, Settings.CacheName);
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
        public async Task<int> CountAsync(string partition, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountPartition, ex, Settings.CacheName, partition);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items stored in the cache.
        /// </summary>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items stored in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        public async Task<long> LongCountAsync(CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex, Settings.CacheName);
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
        public async Task<long> LongCountAsync(string partition, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountPartition, ex, Settings.CacheName, partition);
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
        public async Task<bool> ContainsAsync(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);

            try
            {
                return await ContainsAsyncInternal(partition, key, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
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
        public async Task<CacheResult<TVal>> GetAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
                return default;
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
        public async Task<CacheResult<ICacheItem<TVal>>> GetItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
                return default;
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
        public async Task<IList<ICacheItem<TVal>>> GetItemsAsync<TVal>(CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex, Settings.CacheName);
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
        public async Task<IList<ICacheItem<TVal>>> GetItemsAsync<TVal>(string partition, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnReadPartition, ex, Settings.CacheName, partition);
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
        public async Task<TVal> GetOrAddSlidingAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, TimeSpan interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (valueGetter == null) throw new ArgumentNullException(nameof(valueGetter), ErrorMessages.NullValueGetter);
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == interval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
            }

            return value;
        }

        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given partition and key.
        ///   Value will last as much as specified in
        ///   <see cref="IEssentialCacheSettings.StaticInterval"/> and, if accessed before expiry,
        ///   its lifetime will be extended by that interval.
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
        public async Task<TVal> GetOrAddStaticAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (valueGetter == null) throw new ArgumentNullException(nameof(valueGetter), ErrorMessages.NullValueGetter);
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Settings.StaticInterval);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
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
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, DateTimeOffset utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (valueGetter == null) throw new ArgumentNullException(nameof(valueGetter), ErrorMessages.NullValueGetter);
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
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
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, TimeSpan lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (valueGetter == null) throw new ArgumentNullException(nameof(valueGetter), ErrorMessages.NullValueGetter);
            if (parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem) throw new NotSupportedException(ErrorMessages.TooManyParentKeys);
            if (parentKeys != null && parentKeys.Any(pk => pk == null)) throw new ArgumentException(ErrorMessages.NullKey, nameof(parentKeys));

            if (Log.IsDebugEnabled())
            {
                Log.DebugFormat(DebugMessages.GetItem, partition, key, Settings.CacheName);
            }

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await valueGetter.Invoke().ConfigureAwait(false);
            if (!ReferenceEquals(value, null) && !(Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>())) throw new ArgumentException(ErrorMessages.NotSerializableValue, nameof(value));

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.UtcNow + lifetime, TimeSpan.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
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
        public async Task<CacheResult<TVal>> PeekAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!CanPeek) throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
                return default;
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
        public async Task<CacheResult<ICacheItem<TVal>>> PeekItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);
            if (!CanPeek) throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnRead, ex, partition, key, Settings.CacheName);
                return default;
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
        public async Task<IList<ICacheItem<TVal>>> PeekItemsAsync<TVal>(CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (!CanPeek) throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnReadAll, ex, Settings.CacheName);
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
        public async Task<IList<ICacheItem<TVal>>> PeekItemsAsync<TVal>(string partition, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (!CanPeek) throw new NotSupportedException(string.Format(ErrorMessages.CacheDoesNotAllowPeeking, Settings.CacheName));

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
                Log.ErrorException(ErrorMessages.InternalErrorOnReadPartition, ex, Settings.CacheName, partition);
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
        public async Task RemoveAsync(string partition, string key, CancellationToken cancellationToken = default)
        {
            // Preconditions
            if (Disposed) throw new ObjectDisposedException(Settings.CacheName, string.Format(ErrorMessages.CacheHasBeenDisposed, Settings.CacheName));
            if (partition == null) throw new ArgumentNullException(nameof(partition), ErrorMessages.NullPartition);
            if (key == null) throw new ArgumentNullException(nameof(key), ErrorMessages.NullKey);

            try
            {
                await RemoveAsyncInternal(partition, key, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key));
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnWrite, ex, partition, key, Settings.CacheName);
            }
        }

        #endregion IAsyncCache members

        #region Helpers

        /// <summary>
        ///   Gets a task that has been canceled.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token used to cancel the task.</param>
        /// <returns>A task that has been canceled.</returns>
        private static Task<TResult> CanceledTask<TResult>(CancellationToken cancellationToken = default)
        {
            var tcs = new TaskCompletionSource<TResult>(cancellationToken);
            tcs.TrySetCanceled();
            return tcs.Task;
        }

        #endregion Helpers
    }
}
