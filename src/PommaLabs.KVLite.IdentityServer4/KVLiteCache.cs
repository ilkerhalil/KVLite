// File name: KVLiteCache.cs
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
using PommaLabs.KVLite.Resources;
using System;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.IdentityServer4
{
    /// <summary>
    ///   KVLite-based cache for IdentityServer4.
    /// </summary>
    /// <typeparam name="T">The object type which will be cached.</typeparam>
    public sealed class KVLiteCache<T> : global::IdentityServer4.Services.ICache<T>
        where T : class
    {
        /// <summary>
        ///   Used when receiving null, empty or blank keys.
        /// </summary>
        private const string NoKey = "__none__";

        /// <summary>
        ///   Backing KVLite cache.
        /// </summary>
        private readonly IAsyncCache _cache;

        /// <summary>
        ///   KVLite cache options.
        /// </summary>
        private readonly KVLiteCacheOptions _options;

        /// <summary>
        ///   Initializes cache provider.
        /// </summary>
        /// <param name="cache">Backing KVLite cache.</param>
        /// <param name="options">KVLite cache options.</param>
        public KVLiteCache(IAsyncCache cache, KVLiteCacheOptions options)
        {
            _cache = cache ?? throw new ArgumentNullException(ErrorMessages.NullCache);
            _options = options ?? throw new ArgumentNullException(ErrorMessages.NullSettings);
        }

        /// <summary>
        ///   Gets the cached data based upon a key index.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>The cached item, or <c>null</c> if no item matches the key.</returns>
        public async Task<T> GetAsync(string key)
        {
            var partition = _options.Partition;
            key = string.IsNullOrWhiteSpace(key) ? NoKey : key;
            return (await _cache.GetAsync<T>(partition, key).ConfigureAwait(false)).ValueOrDefault();
        }

        /// <summary>
        ///   Caches the data based upon a key
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="item">The item.</param>
        /// <param name="expiration">The expiration.</param>
        /// <returns>A task.</returns>
        public async Task SetAsync(string key, T item, TimeSpan expiration)
        {
            var partition = _options.Partition;
            key = string.IsNullOrWhiteSpace(key) ? NoKey : key;
            var lifetime = TimeSpan.FromTimeSpan(expiration);
            await _cache.AddTimedAsync(partition, key, item, lifetime).ConfigureAwait(false);
        }
    }
}
