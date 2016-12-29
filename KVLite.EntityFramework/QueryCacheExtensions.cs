// File name: QueryCacheExtensions.cs
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

using EntityFramework.Caching;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace EntityFramework.Extensions
{
    /// <summary>
    ///   More caching extensions.
    /// </summary>
    public static class QueryCacheExtensions
    {
        #region KVLite query cache extensions

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<TEntity> FromTimedCache<TEntity>(this IQueryable<TEntity> query, DateTime utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(new DateTimeOffset(utcExpiry));
            return query.FromCache(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.s
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<TEntity> FromTimedCache<TEntity>(this IQueryable<TEntity> query, DateTimeOffset utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(utcExpiry);
            return query.FromCache(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last for the specified lifetime
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<TEntity> FromTimedCache<TEntity>(this IQueryable<TEntity> query, TimeSpan lifetime, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithDurationExpiration(lifetime);
            return query.FromCache(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "sliding" value, that is, value will last as much as specified in
        ///   given interval and, if accessed before expiry, its lifetime will be extended by the
        ///   interval itself.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static IEnumerable<TEntity> FromSlidingCache<TEntity>(this IQueryable<TEntity> query, TimeSpan interval, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithSlidingExpiration(interval);
            return query.FromCache(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static TEntity FromTimedCacheFirstOrDefault<TEntity>(this IQueryable<TEntity> query, DateTime utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(new DateTimeOffset(utcExpiry));
            return query.FromCacheFirstOrDefault(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.s
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static TEntity FromTimedCacheFirstOrDefault<TEntity>(this IQueryable<TEntity> query, DateTimeOffset utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(utcExpiry);
            return query.FromCacheFirstOrDefault(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last for the specified lifetime
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static TEntity FromTimedCacheFirstOrDefault<TEntity>(this IQueryable<TEntity> query, TimeSpan lifetime, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithDurationExpiration(lifetime);
            return query.FromCacheFirstOrDefault(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "sliding" value, that is, value will last as much as specified in
        ///   given interval and, if accessed before expiry, its lifetime will be extended by the
        ///   interval itself.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static TEntity FromSlidingCacheFirstOrDefault<TEntity>(this IQueryable<TEntity> query, TimeSpan interval, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithSlidingExpiration(interval);
            return query.FromCacheFirstOrDefault(cachePolicy, tags);
        }

        #endregion KVLite query cache extensions

        #region KVLite async query cache extensions

#if !NET40

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<TEntity>> FromTimedCacheAsync<TEntity>(this IQueryable<TEntity> query, DateTime utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(new DateTimeOffset(utcExpiry));
            return query.FromCacheAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.s
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<TEntity>> FromTimedCacheAsync<TEntity>(this IQueryable<TEntity> query, DateTimeOffset utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(utcExpiry);
            return query.FromCacheAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last for the specified lifetime
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<TEntity>> FromTimedCacheAsync<TEntity>(this IQueryable<TEntity> query, TimeSpan lifetime, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithDurationExpiration(lifetime);
            return query.FromCacheAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the result of the query; if possible from the cache, otherwise the query is
        ///   materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "sliding" value, that is, value will last as much as specified in
        ///   given interval and, if accessed before expiry, its lifetime will be extended by the
        ///   interval itself.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>The result of the query.</returns>
        public static Task<IEnumerable<TEntity>> FromSlidingCacheAsync<TEntity>(this IQueryable<TEntity> query, TimeSpan interval, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithSlidingExpiration(interval);
            return query.FromCacheAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static Task<TEntity> FromTimedCacheFirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> query, DateTime utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(new DateTimeOffset(utcExpiry));
            return query.FromCacheFirstOrDefaultAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last until the specified time
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.s
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static Task<TEntity> FromTimedCacheFirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> query, DateTimeOffset utcExpiry, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithAbsoluteExpiration(utcExpiry);
            return query.FromCacheFirstOrDefaultAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "timed" value, that is, value will last for the specified lifetime
        ///   and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="lifetime">The desired lifetime.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static Task<TEntity> FromTimedCacheFirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> query, TimeSpan lifetime, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithDurationExpiration(lifetime);
            return query.FromCacheFirstOrDefaultAsync(cachePolicy, tags);
        }

        /// <summary>
        ///   Returns the first element of the query; if possible from the cache, otherwise the query
        ///   is materialized and the result cached before being returned.
        ///
        ///   Query is cached as a "sliding" value, that is, value will last as much as specified in
        ///   given interval and, if accessed before expiry, its lifetime will be extended by the
        ///   interval itself.
        /// </summary>
        /// <typeparam name="TEntity">The type of the data in the data source.</typeparam>
        /// <param name="query">The query to be materialized.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="tags">The list of tags to use for cache expiration.</param>
        /// <returns>default(T) if source is empty; otherwise, the first element in source.</returns>
        public static Task<TEntity> FromSlidingCacheFirstOrDefaultAsync<TEntity>(this IQueryable<TEntity> query, TimeSpan interval, IEnumerable<string> tags = null)
            where TEntity : class
        {
            var cachePolicy = CachePolicy.WithSlidingExpiration(interval);
            return query.FromCacheFirstOrDefaultAsync(cachePolicy, tags);
        }

#endif

        #endregion KVLite async query cache extensions

        #region DbContext handling

        /// <summary>
        ///   Temporarily configures the given <see cref="DbContext"/> so that caching can work. For
        ///   example, in order to make caching work we need to disable lazy loading and proxy creation.
        ///
        ///   Once the returned object is disposed, the given <see cref="DbContext"/> is restored to
        ///   its original state. We strongly suggest to use this method with a "using" statement.
        /// </summary>
        /// <param name="dbContext">The context on which caching should be enabled.</param>
        /// <returns>An object which can be used to restore the context state.</returns>
#pragma warning disable CC0022 // Should dispose object

        public static IDisposable AsCaching(this DbContext dbContext) => new DbContextReverter(dbContext);

#pragma warning restore CC0022 // Should dispose object

        private sealed class DbContextReverter : IDisposable
        {
            private readonly DbContext _dbContext;
            private readonly bool _oldLazyLoading;
            private readonly bool _oldProxyCreation;
            private bool _disposed;

            public DbContextReverter(DbContext dbContext)
            {
                _dbContext = dbContext;

                _oldLazyLoading = dbContext.Configuration.LazyLoadingEnabled;
                dbContext.Configuration.LazyLoadingEnabled = false;

                _oldProxyCreation = dbContext.Configuration.ProxyCreationEnabled;
                dbContext.Configuration.ProxyCreationEnabled = false;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                if (_dbContext != null)
                {
                    _dbContext.Configuration.LazyLoadingEnabled = _oldLazyLoading;
                    _dbContext.Configuration.ProxyCreationEnabled = _oldProxyCreation;
                }
                _disposed = true;
            }
        }

        #endregion DbContext handling
    }
}