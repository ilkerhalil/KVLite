// File name: AbstractDistributedCache.cs
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

using Microsoft.Extensions.Caching.Distributed;
using NodaTime.Extensions;
using PommaLabs.KVLite.Resources;
using PommaLabs.KVLite.Thrower;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    public abstract partial class AbstractCache<TSettings> : IDistributedCache
    {
        /// <summary>
        ///   The partition used to store all keys received through the
        ///   <see cref="IDistributedCache"/> interface.
        /// </summary>
        public const string DistributedCachePartition = nameof(IDistributedCache);

        byte[] IDistributedCache.Get(string key) => Get<byte[]>(DistributedCachePartition, key).ValueOrDefault();

        async Task<byte[]> IDistributedCache.GetAsync(string key) => (await GetAsync<byte[]>(DistributedCachePartition, key).ConfigureAwait(false)).ValueOrDefault();

        void IDistributedCache.Refresh(string key) => Get<byte[]>(DistributedCachePartition, key);

        async Task IDistributedCache.RefreshAsync(string key) => await GetAsync<byte[]>(DistributedCachePartition, key);

        void IDistributedCache.Remove(string key) => Remove(DistributedCachePartition, key);

        async Task IDistributedCache.RemoveAsync(string key) => await RemoveAsync(DistributedCachePartition, key);

        void IDistributedCache.Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Raise.InvalidOperationException.If(options.SlidingExpiration.HasValue && (options.AbsoluteExpiration.HasValue || options.AbsoluteExpirationRelativeToNow.HasValue), ErrorMessages.CacheDoesNotAllowSlidingAndAbsolute);

            if (options.SlidingExpiration.HasValue)
            {
                AddSliding(DistributedCachePartition, key, value, options.SlidingExpiration.Value.ToDuration());
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                AddTimed(DistributedCachePartition, key, value, options.AbsoluteExpiration.Value.ToInstant());
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                AddTimed(DistributedCachePartition, key, value, options.AbsoluteExpirationRelativeToNow.Value.ToDuration());
            }
        }

        async Task IDistributedCache.SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Raise.InvalidOperationException.If(options.SlidingExpiration.HasValue && (options.AbsoluteExpiration.HasValue || options.AbsoluteExpirationRelativeToNow.HasValue), ErrorMessages.CacheDoesNotAllowSlidingAndAbsolute);

            if (options.SlidingExpiration.HasValue)
            {
                await AddSlidingAsync(DistributedCachePartition, key, value, options.SlidingExpiration.Value.ToDuration()).ConfigureAwait(false);
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                await AddTimedAsync(DistributedCachePartition, key, value, options.AbsoluteExpiration.Value.ToInstant()).ConfigureAwait(false);
            }
            else if (options.AbsoluteExpirationRelativeToNow.HasValue)
            {
                await AddTimedAsync(DistributedCachePartition, key, value, options.AbsoluteExpirationRelativeToNow.Value.ToDuration()).ConfigureAwait(false);
            }
        }
    }
}
