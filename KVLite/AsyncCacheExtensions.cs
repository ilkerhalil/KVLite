// File name: AsyncCacheExtensions.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Async extension methods for <see cref="ICache"/>.
    /// </summary>
    public static class AsyncCacheExtensions
    {
        /// <summary>
        ///   At first, it tries to get the cache item with specified partition and key. If it is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        ///
        ///   If the value is not found, then it adds a "timed" value with given partition and key.
        ///   Value will last for the specified lifetime and, if accessed before expiry, its lifetime
        ///   will _not_ be extended.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="asyncValueGetter">
        ///   The function that is called in order to get the value when it was not found inside the cache.
        /// </param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        /// <returns>
        ///   The value found in the cache or the one returned by
        ///   <paramref name="asyncValueGetter"/>, in case a new value has been added to the cache.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="partition"/>, <paramref name="key"/> or
        ///   <paramref name="asyncValueGetter"/> are null.
        /// </exception>
        /// <exception cref="NotSupportedException">
        ///   Too many parent keys have been specified for this item. Please have a look at the
        ///   <see cref="ICache.MaxParentKeyCountPerItem"/> to understand how many parent keys each
        ///   item may have.
        /// </exception>
#if !NET40
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
#endif

        public static async Task<TVal> GetOrAddTimedAsync<TVal>(this ICache cache, string partition, string key, Func<Task<TVal>> asyncValueGetter, TimeSpan lifetime, IList<string> parentKeys = null)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            RaiseObjectDisposedException.If(cache.Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentNullException.IfIsNull(key, nameof(key), ErrorMessages.NullKey);
            Raise.ArgumentNullException.IfIsNull(asyncValueGetter, nameof(asyncValueGetter), ErrorMessages.NullValueGetter);
            RaiseNotSupportedException.If(parentKeys != null && parentKeys.Count > cache.MaxParentKeyCountPerItem, ErrorMessages.TooManyParentKeys);
            RaiseArgumentException.If(parentKeys != null && parentKeys.Any(pk => pk == null), nameof(parentKeys), ErrorMessages.NullKey);

            try
            {
                var result = cache.Get<TVal>(partition, key);

                // Postconditions
                Debug.Assert(cache.Contains(partition, key) == result.HasValue);
                if (result.HasValue)
                {
                    return result.Value;
                }
            }
            catch (Exception ex)
            {
                cache.LastError = ex;
                cache.Log.Error(string.Format(ErrorMessages.InternalErrorOnRead, partition, key), ex);
            }

            // This line is reached when the cache does not contain the item or an error has occurred.
            var value = await asyncValueGetter.Invoke();
            RaiseArgumentException.IfNot(ReferenceEquals(value, null) || (cache.Serializer.CanSerialize(value.GetType()) && cache.Serializer.CanDeserialize(value.GetType())), nameof(value), ErrorMessages.NotSerializableValue);

            try
            {
                cache.AddTimed(partition, key, value, lifetime, parentKeys);

                // Postconditions
                Debug.Assert(!cache.Contains(cache.Settings.DefaultPartition, key) || !cache.CanPeek || cache.PeekItem<TVal>(cache.Settings.DefaultPartition, key).Value.Interval == TimeSpan.Zero);
            }
            catch (Exception ex)
            {
                cache.LastError = ex;
                cache.Log.Error(string.Format(ErrorMessages.InternalErrorOnWrite, partition, key), ex);
            }

            return value;
        }
    }
}