// File name: AsyncCacheExtensions.cs
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

using NodaTime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Extensions methods for <see cref="IAsyncCache"/>.
    /// </summary>
    public static class AsyncCacheExtensions
    {
        #region Add

        /// <summary>
        ///   Adds a "sliding" value with given key and default partition. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task AddSlidingAsync<TVal>(this IAsyncCache cache, string key, TVal value, Duration interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.AddSlidingAsync(cache.Settings.DefaultPartition, key, value, interval, parentKeys, cancellationToken);

        /// <summary>
        ///   Adds a "static" value with given key and default partition. Value will last as much as
        ///   specified in <see cref="IEssentialCacheSettings.StaticInterval"/> and, if
        ///   accessed before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task AddStaticAsync<TVal>(this IAsyncCache cache, string key, TVal value, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.AddStaticAsync(cache.Settings.DefaultPartition, key, value, parentKeys, cancellationToken);

        /// <summary>
        ///   Adds a "timed" value with given key and default partition. Value will last until the
        ///   specified time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task AddTimedAsync<TVal>(this IAsyncCache cache, string key, TVal value, Instant utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.AddTimedAsync(cache.Settings.DefaultPartition, key, value, utcExpiry, parentKeys, cancellationToken);

        /// <summary>
        ///   Adds a "timed" value with given key and default partition. Value will last for the
        ///   specified lifetime and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task AddTimedAsync<TVal>(this IAsyncCache cache, string key, TVal value, Duration lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.AddTimedAsync(cache.Settings.DefaultPartition, key, value, lifetime, parentKeys, cancellationToken);

        #endregion Add

        #region Contains

        /// <summary>
        ///   Determines whether this cache contains the specified key in the default partition.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>Whether this cache contains the specified key in the default partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public static Task<bool> ContainsAsync(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.ContainsAsync(cache.Settings.DefaultPartition, key, cancellationToken);

        #endregion Contains

        #region Get & GetItem(s)

        /// <summary>
        ///   Gets the value with default partition and specified key. If it is a "sliding" or
        ///   "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <returns>The value with default partition and specified key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public static Task<CacheResult<TVal>> GetAsync<TVal>(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetAsync<TVal>(cache.Settings.DefaultPartition, key, cancellationToken);

        /// <summary>
        ///   Gets the cache item with default partition and specified key. If it is a "sliding" or
        ///   "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <returns>The cache item with default partition and specified key.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public static Task<CacheResult<ICacheItem<TVal>>> GetItemAsync<TVal>(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetItemAsync<TVal>(cache.Settings.DefaultPartition, key, cancellationToken);

        #endregion Get & GetItem(s)

        #region GetOrAdd

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "sliding" value with given key and default
        ///   partition. Value will last as much as specified in given interval and, if accessed
        ///   before expiry, its lifetime will be extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
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
        ///   <paramref name="key"/> or <paramref name="valueGetter"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task<TVal> GetOrAddSlidingAsync<TVal>(this IAsyncCache cache, string key, Func<Task<TVal>> valueGetter, Duration interval, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetOrAddSlidingAsync(cache.Settings.DefaultPartition, key, valueGetter, interval, parentKeys, cancellationToken);

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "static" value with given key and default
        ///   partition. Value will last as much as specified in
        ///   <see cref="IEssentialCacheSettings.StaticInterval"/> and, if accessed before
        ///   expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
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
        ///   <paramref name="key"/> or <paramref name="valueGetter"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task<TVal> GetOrAddStaticAsync<TVal>(this IAsyncCache cache, string key, Func<Task<TVal>> valueGetter, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetOrAddStaticAsync(cache.Settings.DefaultPartition, key, valueGetter, parentKeys, cancellationToken);

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given key and default
        ///   partition. Value will last until the specified time and, if accessed before expiry, its
        ///   lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
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
        ///   <paramref name="key"/> or <paramref name="valueGetter"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task<TVal> GetOrAddTimedAsync<TVal>(this IAsyncCache cache, string key, Func<Task<TVal>> valueGetter, Instant utcExpiry, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetOrAddTimedAsync(cache.Settings.DefaultPartition, key, valueGetter, utcExpiry, parentKeys, cancellationToken);

        /// <summary>
        ///   At first, it tries to get the cache item with default partition and specified key. If
        ///   it is a "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given key and default
        ///   partition. Value will last for the specified lifetime and, if accessed before expiry,
        ///   its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The async cache.</param>
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
        ///   <paramref name="key"/> or <paramref name="valueGetter"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="IEssentialCache.MaxParentKeyCountPerItem"/> to understand how many parent
        ///   keys each item may have.
        /// </exception>
        public static Task<TVal> GetOrAddTimedAsync<TVal>(this IAsyncCache cache, string key, Func<Task<TVal>> valueGetter, Duration lifetime, IList<string> parentKeys = null, CancellationToken cancellationToken = default(CancellationToken))
            => cache.GetOrAddTimedAsync(cache.Settings.DefaultPartition, key, valueGetter, lifetime, parentKeys, cancellationToken);

        #endregion GetOrAdd

        #region Peek & PeekItem(s)

        /// <summary>
        ///   Gets the value corresponding to default partition and given key, without updating
        ///   expiry date.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>
        ///   The value corresponding to default partition and given key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public static Task<CacheResult<TVal>> PeekAsync<TVal>(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.PeekAsync<TVal>(cache.Settings.DefaultPartition, key, cancellationToken);

        /// <summary>
        ///   Gets the item corresponding to default partition and given key, without updating expiry date.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>
        ///   The item corresponding to default partition and givne key, without updating expiry date.
        /// </returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        /// <exception cref="NotSupportedException">
        ///   Cache does not support peeking (please have a look at the
        ///   <see cref="IEssentialCache.CanPeek"/> property).
        /// </exception>
        public static Task<CacheResult<ICacheItem<TVal>>> PeekItemAsync<TVal>(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.PeekItemAsync<TVal>(cache.Settings.DefaultPartition, key, cancellationToken);

        #endregion Peek & PeekItem(s)

        #region Remove

        /// <summary>
        ///   Removes the item with specified key from default partition.
        /// </summary>
        /// <param name="cache">The async cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <exception cref="ArgumentNullException"><paramref name="key"/> is null.</exception>
        public static Task RemoveAsync(this IAsyncCache cache, string key, CancellationToken cancellationToken = default(CancellationToken))
            => cache.RemoveAsync(cache.Settings.DefaultPartition, key, cancellationToken);

        #endregion Remove
    }
}
