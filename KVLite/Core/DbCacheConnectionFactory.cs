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
    public abstract class DbCacheConnectionFactory : IDbCacheConnectionFactory
    {
        private static readonly Regex SqlNameRegex = new Regex("[a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly DbProviderFactory _dbProviderFactory;

        protected DbCacheConnectionFactory(DbProviderFactory dbProviderFactory, string cacheSchemaName, string cacheItemsTableName, string cacheValuesTableName)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(dbProviderFactory, nameof(dbProviderFactory));
            Raise.ArgumentException.If(cacheSchemaName != null && !SqlNameRegex.IsMatch(cacheSchemaName));
            Raise.ArgumentException.If(cacheItemsTableName != null && !SqlNameRegex.IsMatch(cacheItemsTableName));
            Raise.ArgumentException.If(cacheValuesTableName != null && !SqlNameRegex.IsMatch(cacheValuesTableName));

            _dbProviderFactory = dbProviderFactory;

            CacheSchemaName = cacheSchemaName ?? DefaultCacheSchemaName;
            CacheItemsTableName = cacheItemsTableName ?? DefaultCacheItemsTableName;
            CacheValuesTableName = cacheValuesTableName ?? DefaultCacheValuesTableName;
        }

        public static string DefaultCacheSchemaName { get; } = "kvlite";

        public static string DefaultCacheItemsTableName { get; } = "kvl_cache_items";

        public static string DefaultCacheValuesTableName { get; } = "kvl_cache_values";

        public string CacheSchemaName { get; set; }

        public string CacheItemsTableName { get; set; }

        public string CacheValuesTableName { get; set; }

        public int MaxKeyNameLength { get; } = 255;

        public int MaxPartitionNameLength { get; } = 255;

        #region Commands

        public string InsertOrUpdateCacheItemCommand { get; protected set; }

        public string DeleteCacheItemCommand { get; protected set; }

        public string DeleteCacheItemsCommand { get; protected set; }

        public string UpdateCacheItemExpiryCommand { get; protected set; }

        #endregion Commands

        #region Queries

        public string ContainsCacheItemQuery { get; protected set; }

        public string CountCacheItemsQuery { get; protected set; }

        public string PeekCacheItemsQuery { get; protected set; }

        public string PeekCacheItemQuery { get; protected set; }

        public string PeekCacheValueQuery { get; protected set; }

        #endregion Queries

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        public virtual string ConnectionString { get; set; }

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <returns>An open connection.</returns>
        public virtual DbConnection Open()
        {
            var connection = _dbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
            connection.Open();
            return connection;
        }

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        public virtual async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = _dbProviderFactory.CreateConnection();
            connection.ConnectionString = ConnectionString;
#if !NET40
            await connection.OpenAsync(cancellationToken);
#else
            connection.Open();
#endif
            return connection;
        }

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        public abstract long GetCacheSizeInBytes();

#region Private Methods

        protected static string MinifyQuery(string query)
        {
            // Removes all SQL comments. Multiline excludes '/n' from '.' matches.
            query = Regex.Replace(query, @"--.*", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled);

            // Removes all multiple blanks.
            query = Regex.Replace(query, @"\s+", " ", RegexOptions.Compiled);

            // Removes initial and ending blanks.
            return query.Trim();
        }

#endregion Private Methods
    }
}