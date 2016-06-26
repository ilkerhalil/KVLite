// File name: QueryCacheProvider.cs
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

using EntityFramework;
using EntityFramework.Caching;
using Finsa.CodeServices.Caching;
using Finsa.CodeServices.Common;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.EntityFramework
{
    /// <summary>
    ///   KVLite-based query cache for Entity Framework.
    /// </summary>
    public sealed class QueryCacheProvider : ICacheProvider
    {
        #region Constants

        /// <summary>
        ///   The partition used by EF cache provider items.
        /// </summary>
        const string EfCachePartition = "KVL.EF.QCP";

        #endregion Constants

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="QueryCacheProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache that will be used as entry container.</param>
        public QueryCacheProvider(ICache cache)
        {
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        #endregion Construction

        #region Public members

        /// <summary>
        ///   Gets the underlying cache.
        /// </summary>
        /// <value>The underlying cache.</value>
        public ICache Cache { get; }

        /// <summary>
        ///   Registers this class as the default query cache provider.
        /// </summary>
        /// <param name="cache">The underlying cache.</param>
        public static void Register(ICache cache)
        {
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Locator.Current.Register<ICacheProvider>(() => new QueryCacheProvider(cache));
        }

        /// <summary>
        ///   Registers this class as the default query cache provider.
        /// </summary>
        /// <param name="cacheResolver">The resolver used to get the underlying cache.</param>
        public static void Register(Func<ICache> cacheResolver)
        {
            Raise.ArgumentNullException.IfIsNull(cacheResolver, nameof(cacheResolver), ErrorMessages.NullCacheResolver);
            Locator.Current.Register<ICacheProvider>(() => new QueryCacheProvider(cacheResolver?.Invoke()));
        }

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
            if (Cache.Contains(EfCachePartition, cacheKey.Key))
            {
                // Cache already contains an entry for given key.
                return false;
            }
            // Cache does not contain given key, then we should add it.
            return Set(cacheKey, value, cachePolicy);
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
            Cache.Remove(EfCachePartition, cacheTag.ToString());
            return 0; // KVLite does not expose that information.
        }

        /// <summary>
        ///   Gets the cache value for the specified key
        /// </summary>
        /// <param name="cacheKey">A unique identifier for the cache entry.</param>
        /// <returns>
        ///   The cache value for the specified key, if the entry exists; otherwise, <see langword="null"/>.
        /// </returns>
        public object Get(CacheKey cacheKey) => Cache.Get<object>(EfCachePartition, cacheKey.Key).ValueOrDefault();

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
            // We need to add the tags to the cache, so that we can use them as parent keys.
            var parentKeys = AddTagsToCache(cacheKey.Tags);

            switch (cachePolicy.Mode)
            {
                case CacheExpirationMode.Absolute:
                    // The cache item will expire on the AbsoluteExpiration DateTime.
                    var utcExpiry = cachePolicy.AbsoluteExpiration.DateTime.ToUniversalTime();
                    return Cache.GetOrAddTimed(EfCachePartition, cacheKey.Key, () => valueFactory?.Invoke(cacheKey), utcExpiry, parentKeys);

                case CacheExpirationMode.Duration:
                    // The cache item will expire using the Duration property to calculate the
                    // absolute expiration from DateTimeOffset.Now.
                    return Cache.GetOrAddTimed(EfCachePartition, cacheKey.Key, () => valueFactory?.Invoke(cacheKey), cachePolicy.Duration, parentKeys);

                case CacheExpirationMode.None:
                    // The cache item will not expire.
                    return Cache.GetOrAddStatic(EfCachePartition, cacheKey.Key, () => valueFactory?.Invoke(cacheKey), parentKeys);

                case CacheExpirationMode.Sliding:
                    // The cache item will expire using the SlidingExpiration property as the sliding expiration.
                    return Cache.GetOrAddSliding(EfCachePartition, cacheKey.Key, () => valueFactory?.Invoke(cacheKey), cachePolicy.SlidingExpiration, parentKeys);

                default:
                    // Should never execute line below...
                    return valueFactory?.Invoke(cacheKey);
            }
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
#if NET40
            return TaskEx.FromResult(GetOrAdd(cacheKey, ck => valueFactory?.Invoke(ck).Result, cachePolicy));
#else
            return Task.FromResult(GetOrAdd(cacheKey, ck => valueFactory?.Invoke(ck).Result, cachePolicy));
#endif
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
            var maybeValue = Cache.Get<object>(EfCachePartition, cacheKey.Key);
            Cache.Remove(EfCachePartition, cacheKey.Key);
            return maybeValue.ValueOrDefault();
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
            // We need to add the tags to the cache, so that we can use them as parent keys.
            var parentKeys = AddTagsToCache(cacheKey.Tags);

            switch (cachePolicy.Mode)
            {
                case CacheExpirationMode.Absolute:
                    // The cache item will expire on the AbsoluteExpiration DateTime.
                    var utcExpiry = cachePolicy.AbsoluteExpiration.DateTime.ToUniversalTime();
                    Cache.AddTimed(EfCachePartition, cacheKey.Key, value, utcExpiry, parentKeys);
                    break;

                case CacheExpirationMode.Duration:
                    // The cache item will expire using the Duration property to calculate the
                    // absolute expiration from DateTimeOffset.Now.
                    Cache.AddTimed(EfCachePartition, cacheKey.Key, value, cachePolicy.Duration, parentKeys);
                    break;

                case CacheExpirationMode.None:
                    // The cache item will not expire.
                    Cache.AddStatic(EfCachePartition, cacheKey.Key, value, parentKeys);
                    break;

                case CacheExpirationMode.Sliding:
                    // The cache item will expire using the SlidingExpiration property as the sliding expiration.
                    Cache.AddSliding(EfCachePartition, cacheKey.Key, value, cachePolicy.SlidingExpiration, parentKeys);
                    break;
            }

            // The entry has been added.
            return true;
        }

#endregion ICacheProvider members

        #region Private methods

        /// <summary>
        ///   Adds the tags to the cache, so that they can be used as parent keys.
        /// </summary>
        /// <param name="tags">The tags to be added.</param>
        /// <returns>The tags converted into a format accepted by KVLite.</returns>
        private string[] AddTagsToCache(HashSet<CacheTag> tags)
        {
            var parentKeys = new string[tags?.Count ?? 0];
            var index = 0;
            foreach (var tag in tags)
            {
                var parentKey = tag.ToString();
                parentKeys[index] = parentKey;

                // Adds the tag as a static item, so that it will not be deleted in a reasonable amount of time.
                Cache.AddStatic(EfCachePartition, parentKey, parentKey);

                index++;
            }
            return parentKeys;
        }

        #endregion
    }
}
