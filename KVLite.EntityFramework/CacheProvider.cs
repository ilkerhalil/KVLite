// File name: CacheProvider.cs
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

using EntityFramework.Caching;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.EntityFramework
{
    /// <summary>
    ///   KVLite-based query cache for Entity Framework.
    /// </summary>
    public sealed class CacheProvider : ICacheProvider
    {
        #region Constants

        /// <summary>
        ///   The partition used by EF cache provider items.
        /// </summary>
        const string EfCachePartition = "KVLite.EntityFramework.CacheProvider";

        #endregion Constants

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="CacheProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache that will be used as entry container.</param>
        public CacheProvider(ICache cache)
        {
            RaiseArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        #endregion Construction

        #region Public members

        /// <summary>
        ///   Gets the underlying cache.
        /// </summary>
        /// <value>The underlying cache.</value>
        public ICache Cache { get; }

        #endregion Public members

        #region ICacheProvider members

        /// <summary>
        ///   Inserts a cache entry into the cache without overwriting any existing cache entry.
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        /// <param name="cachePolicy">
        ///   An object that contains eviction details for the cache entry.
        /// </param>
        /// <returns>
        ///   <c>true</c> if insertion succeeded, or <c>false</c> if there is an already an entry in
        ///   the cache that has the same key as key.
        /// </returns>
        public bool Add(CacheKey cacheKey, object value, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Clears all entries from the cache
        /// </summary>
        /// <returns>The number of items removed.</returns>
        public long ClearCache() => Cache.Clear(EfCachePartition);

        /// <summary>
        ///   Expires the specified cache tag.
        /// </summary>
        /// <param name="cacheTag">The cache tag.</param>
        /// <returns>The number of items expired.</returns>
        public int Expire(CacheTag cacheTag)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets the cache value for the specified key
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <returns>
        ///   The cache value for the specified key, if the entry exists; otherwise, <see langword="null"/>.
        /// </returns>
        public object Get(CacheKey cacheKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets the cache value for the specified key that is already in the dictionary or the
        ///   new value for the key as returned by <paramref name="valueFactory"/>.
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <param name="valueFactory">
        ///   The function used to generate a value to insert into cache.
        /// </param>
        /// <param name="cachePolicy">
        ///   A <see cref="T:EntityFramework.Caching.CachePolicy"/> that contains eviction details
        ///   for the cache entry.
        /// </param>
        /// <returns>
        ///   The value for the key. This will be either the existing value for the key if the key
        ///   is already in the cache, or the new value for the key as returned by
        ///   <paramref name="valueFactory"/> if the key was not in the cache.
        /// </returns>
        public object GetOrAdd(CacheKey cacheKey, Func<CacheKey, object> valueFactory, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Gets the cache value for the specified key that is already in the dictionary or the
        ///   new value for the key as returned asynchronously by <paramref name="valueFactory"/>.
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <param name="valueFactory">
        ///   The asynchronous function used to generate a value to insert into cache.
        /// </param>
        /// <param name="cachePolicy">
        ///   A <see cref="T:EntityFramework.Caching.CachePolicy"/> that contains eviction details
        ///   for the cache entry.
        /// </param>
        /// <returns>
        ///   The value for the key. This will be either the existing value for the key if the key
        ///   is already in the cache, or the new value for the key as returned by
        ///   <paramref name="valueFactory"/> if the key was not in the cache.
        /// </returns>
        public Task<object> GetOrAddAsync(CacheKey cacheKey, Func<CacheKey, Task<object>> valueFactory, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Removes a cache entry from the cache.
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <returns>
        ///   If the entry is found in the cache, the removed cache entry; otherwise, <see langword="null"/>.
        /// </returns>
        public object Remove(CacheKey cacheKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Inserts a cache entry into the cache overwriting any existing cache entry.
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <param name="value">The object to insert.</param>
        /// <param name="cachePolicy">
        ///   A <see cref="T:EntityFramework.Caching.CachePolicy"/> that contains eviction details
        ///   for the cache entry.
        /// </param>
        public bool Set(CacheKey cacheKey, object value, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        #endregion ICacheProvider members
    }
}
