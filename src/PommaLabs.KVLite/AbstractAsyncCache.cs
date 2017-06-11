// File name: AbstractAsyncCache.cs
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

using NodaTime;
using PommaLabs.KVLite.Resources;
using PommaLabs.Thrower;
using PommaLabs.KVLite.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    public abstract partial class AbstractCache<TSettings> : IAsyncCache<TSettings>
    {
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

        Task<CacheResult<object>> IAsyncCache.this[string partition, string key]
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
        public async Task AddSlidingAsync<TVal>(string partition, string key, TVal value, Duration interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

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
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

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
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, Instant utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, Duration.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Duration.Zero);
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
        public async Task AddTimedAsync<TVal>(string partition, string key, TVal value, Duration lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);
            Raise.NotSupportedException.If(parentKeys != null && parentKeys.Count > MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            Raise.ArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + lifetime, Duration.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Duration.Zero);
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
        public async Task<CacheResult<TVal>> GetAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
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
                return default(CacheResult<TVal>);
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
        public async Task<CacheResult<ICacheItem<TVal>>> GetItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
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
                return default(CacheResult<ICacheItem<TVal>>);
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
        public async Task<TVal> GetOrAddSlidingAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, Duration interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
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
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + interval, interval, parentKeys, cancellationToken).ConfigureAwait(false);

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
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + Settings.StaticInterval, Settings.StaticInterval, parentKeys, cancellationToken).ConfigureAwait(false);

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
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, Instant utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
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
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, utcExpiry, Duration.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Duration.Zero);
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
        public async Task<TVal> GetOrAddTimedAsync<TVal>(string partition, string key, Func<Task<TVal>> valueGetter, Duration lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
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
            Raise.ArgumentException.IfNot(ReferenceEquals(value, null) || (Serializer.CanSerialize<TVal>() && Serializer.CanDeserialize<TVal>()), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                await AddAsyncInternal(partition, key, value, Clock.GetCurrentInstant() + lifetime, Duration.Zero, parentKeys, cancellationToken).ConfigureAwait(false);

                // Postconditions
                Debug.Assert(!Contains(partition, key) || !CanPeek || PeekItem<TVal>(partition, key).Value.Interval == Duration.Zero);
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
        public async Task<CacheResult<TVal>> PeekAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
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
                return default(CacheResult<TVal>);
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
        public async Task<CacheResult<ICacheItem<TVal>>> PeekItemAsync<TVal>(string partition, string key, CancellationToken cancellationToken = default(CancellationToken))
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
                return default(CacheResult<ICacheItem<TVal>>);
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
    }
}
