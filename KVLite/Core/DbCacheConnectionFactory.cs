// File name: DbCacheConnectionFactory.cs
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

using PommaLabs.Thrower;
using System.Data.Common;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for cache connection factories.
    /// </summary>
    public abstract class DbCacheConnectionFactory : IDbCacheConnectionFactory
    {
        private readonly DbProviderFactory _dbProviderFactory;

        /// <summary>
        ///   Initializes the cache connection factory.
        /// </summary>
        /// <param name="dbProviderFactory">Provider used to open connection.</param>
        /// <param name="cacheSchemaName">Optional cache schema name.</param>
        /// <param name="cacheEntriesTableName">Optional cache entries table name.</param>
        protected DbCacheConnectionFactory(DbProviderFactory dbProviderFactory, string cacheSchemaName, string cacheEntriesTableName)
        {
            var sqlNameRegex = new Regex("[a-z0-9_]+", RegexOptions.IgnoreCase);

            // Preconditions
            Raise.ArgumentNullException.IfIsNull(dbProviderFactory, nameof(dbProviderFactory));
            Raise.ArgumentException.If(cacheSchemaName != null && !sqlNameRegex.IsMatch(cacheSchemaName));
            Raise.ArgumentException.If(cacheEntriesTableName != null && !sqlNameRegex.IsMatch(cacheEntriesTableName));

            _dbProviderFactory = dbProviderFactory;

            CacheSchemaName = cacheSchemaName ?? DefaultCacheSchemaName;
            CacheEntriesTableName = cacheEntriesTableName ?? DefaultCacheEntriesTableName;
        }

        #region Configuration

        /// <summary>
        ///   Default cache schema name.
        /// </summary>
        public static string DefaultCacheSchemaName { get; } = "kvlite";

        /// <summary>
        ///   Default cache entries table name.
        /// </summary>
        public static string DefaultCacheEntriesTableName { get; } = "kvl_cache_entries";

        /// <summary>
        ///   The schema which holds cache entries table.
        /// </summary>
        public string CacheSchemaName { get; set; }

        /// <summary>
        ///   The name of the table which holds cache entries.
        /// </summary>
        public string CacheEntriesTableName { get; set; }

        /// <summary>
        ///   The maximum length a partition can have. Longer partitions will be truncated.
        /// </summary>
        public int MaxPartitionNameLength { get; } = 255;

        /// <summary>
        ///   The maximum length a key can have. Longer keys will be truncated.
        /// </summary>
        public int MaxKeyNameLength { get; } = 255;

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        public virtual string ConnectionString { get; set; }

        #endregion Configuration

        #region Commands

        /// <summary>
        ///   Inserts or updates a cache entry.
        /// </summary>
        public string InsertOrUpdateCacheEntryCommand { get; protected set; }

        /// <summary>
        ///   Deletes a cache entry using specified hash.
        /// </summary>
        public string DeleteCacheEntryCommand { get; protected set; }

        /// <summary>
        ///   Deletes a set of cache entries.
        /// </summary>
        public string DeleteCacheEntriesCommand { get; protected set; }

        /// <summary>
        ///   Updates the expiry time of one cache entry.
        /// </summary>
        public string UpdateCacheEntryExpiryCommand { get; protected set; }

        #endregion Commands

        #region Queries

        /// <summary>
        ///   Checks whether a valid entry with given hash exists.
        /// </summary>
        public string ContainsCacheEntryQuery { get; protected set; }

        /// <summary>
        ///   Counts valid or invalid cache entries.
        /// </summary>
        public string CountCacheEntriesQuery { get; protected set; }

        /// <summary>
        ///   Returns all valid cache entries without updating their expiry time.
        /// </summary>
        public string PeekCacheEntriesQuery { get; protected set; }

        /// <summary>
        ///   Returns a valid cache entry with given hash.
        /// </summary>
        public string PeekCacheEntryQuery { get; protected set; }

        /// <summary>
        ///   Returns a valid cache value with given hash.
        /// </summary>
        public string PeekCacheValueQuery { get; protected set; }

        /// <summary>
        ///   Returns current cache size in bytes.
        /// </summary>
        public string GetCacheSizeInBytesQuery { get; protected set; }

        #endregion Queries

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <returns>An open connection.</returns>
        public DbConnection Open()
        {
            var connection = _dbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();
            return connection;
        }

#if !NET40

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = _dbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            await connection.OpenAsync(cancellationToken);
            return connection;
        }

#else

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        public Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
        {
            return TaskEx.FromResult(Open());
        }

#endif

        #region Query optimization

        /// <summary>
        ///   Minifies specified query.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <returns>A query without comments and unnecessary blank characters.</returns>
        protected static string MinifyQuery(string query)
        {
            // Removes all SQL comments. Multiline excludes '/n' from '.' matches.
            query = Regex.Replace(query, @"--.*", string.Empty, RegexOptions.Multiline);

            // Removes all multiple blanks.
            query = Regex.Replace(query, @"\s+", " ");

            // Removes initial and ending blanks.
            return query.Trim();
        }

        #endregion Query optimization
    }
}