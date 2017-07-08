// File name: DbCacheConnectionFactory.cs
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

using PommaLabs.KVLite.Thrower;
using System.Data.Common;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Database
{
    /// <summary>
    ///   Base class for cache connection factories.
    /// </summary>
    /// <typeparam name="TConnection">The type of the cache connection.</typeparam>
    public abstract class DbCacheConnectionFactory<TConnection>
        where TConnection : DbConnection
    {
        /// <summary>
        ///   Used to validate SQL names.
        /// </summary>
        private static Regex IsValidSqlNameRegex { get; } = new Regex("[a-z0-9_]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private readonly DbProviderFactory _dbProviderFactory;
        private string _cacheSchemaName;
        private string _cacheEntriesTableName;

        /// <summary>
        ///   Initializes the cache connection factory.
        /// </summary>
        /// <param name="dbProviderFactory">Provider used to open connection.</param>
        /// <param name="cacheSchemaName">Optional cache schema name.</param>
        /// <param name="cacheEntriesTableName">Optional cache entries table name.</param>
        protected DbCacheConnectionFactory(DbProviderFactory dbProviderFactory, string cacheSchemaName, string cacheEntriesTableName)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(dbProviderFactory, nameof(dbProviderFactory));
            Raise.ArgumentException.If(cacheSchemaName != null && !IsValidSqlNameRegex.IsMatch(cacheSchemaName));
            Raise.ArgumentException.If(cacheEntriesTableName != null && !IsValidSqlNameRegex.IsMatch(cacheEntriesTableName));

            _dbProviderFactory = dbProviderFactory;

            _cacheSchemaName = cacheSchemaName ?? DefaultCacheSchemaName;
            _cacheEntriesTableName = cacheEntriesTableName ?? DefaultCacheEntriesTableName;

#pragma warning disable RECS0021 // Warns about calls to virtual member functions occuring in the constructor
            UpdateCommandsAndQueries();
#pragma warning restore RECS0021 // Warns about calls to virtual member functions occuring in the constructor
        }

        #region Configuration

        /// <summary>
        ///   Default cache schema name.
        /// </summary>
        public static string DefaultCacheSchemaName { get; } = string.Empty;

        /// <summary>
        ///   Default cache entries table name.
        /// </summary>
        public static string DefaultCacheEntriesTableName { get; } = "kvl_cache_entries";

        /// <summary>
        ///   The schema which holds cache entries table.
        /// </summary>
        public string CacheSchemaName
        {
            get
            {
                var result = _cacheSchemaName;

                // Postconditions
                Debug.Assert(IsValidSqlNameRegex.IsMatch(result));
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentException.IfNot(IsValidSqlNameRegex.IsMatch(value));

                _cacheSchemaName = value;
                UpdateCommandsAndQueries();
            }
        }

        /// <summary>
        ///   The name of the table which holds cache entries.
        /// </summary>
        public string CacheEntriesTableName
        {
            get
            {
                var result = _cacheEntriesTableName;

                // Postconditions
                Debug.Assert(IsValidSqlNameRegex.IsMatch(result));
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentException.IfNot(IsValidSqlNameRegex.IsMatch(value));

                _cacheEntriesTableName = value;
                UpdateCommandsAndQueries();
            }
        }

        /// <summary>
        ///   The maximum length a partition can have. Longer partitions will be truncated. Default
        ///   value is 2000, but each SQL connection factory might change it.
        /// </summary>
        public int MaxPartitionNameLength { get; } = 2000;

        /// <summary>
        ///   The maximum length a key can have. Longer keys will be truncated. Default value is
        ///   2000, but each SQL connection factory might change it.
        /// </summary>
        public int MaxKeyNameLength { get; } = 2000;

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
        public virtual TConnection Open()
        {
            var connection = _dbProviderFactory.CreateConnection() as TConnection;
            connection.ConnectionString = ConnectionString;
            connection.Open();
            return connection;
        }

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        public virtual async Task<TConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var connection = _dbProviderFactory.CreateConnection() as TConnection;
            connection.ConnectionString = ConnectionString;
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }

        #region Query optimization

        /// <summary>
        ///   Function used to estimate cache size.
        /// </summary>
        protected virtual string LengthSqlFunction { get; } = "length";

        /// <summary>
        ///   Prefix used to identify parameters inside a SQL query or command.
        /// </summary>
        protected virtual string ParameterPrefix { get; } = "@";

        /// <summary>
        ///   The symbol used to enclose an identifier (left side).
        /// </summary>
        protected virtual string LeftIdentifierEncloser { get; } = string.Empty;

        /// <summary>
        ///   The symbol used to enclose an identifier (right side).
        /// </summary>
        protected virtual string RightIdentifierEncloser { get; } = string.Empty;

        /// <summary>
        ///   SQL schema including DOT character. If schema was empty, then this property will be empty.
        /// </summary>
        protected virtual string SqlSchemaWithDot => string.IsNullOrEmpty(CacheSchemaName) ? string.Empty : $"{CacheSchemaName}.";

        /// <summary>
        ///   This method is called when either the cache schema name or the cache entries table name
        ///   have been changed by the user.
        /// </summary>
        protected virtual void UpdateCommandsAndQueries()
        {
            var p = ParameterPrefix;
            var l = LeftIdentifierEncloser;
            var r = RightIdentifierEncloser;
            var s = SqlSchemaWithDot;
            var t = uint.MaxValue;

            #region Commands

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {s}{CacheEntriesTableName}
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheEntry.Single.Hash)}
                    or {DbCacheEntry.ParentHash0Column} = {p}{nameof(DbCacheEntry.Single.Hash)}
                    or {DbCacheEntry.ParentHash1Column} = {p}{nameof(DbCacheEntry.Single.Hash)}
                    or {DbCacheEntry.ParentHash2Column} = {p}{nameof(DbCacheEntry.Single.Hash)}
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {s}{CacheEntriesTableName}
                 where ({p}{nameof(DbCacheEntry.Group.Hash)} is null or ({DbCacheValue.HashColumn} >= {p}{nameof(DbCacheEntry.Group.Hash)} and {DbCacheValue.HashColumn} <= {p}{nameof(DbCacheEntry.Group.Hash)} + {t}))
                   and (({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or {DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
                     or ({DbCacheEntry.ParentHash0Column} in (select x.{DbCacheValue.HashColumn} from
                                                             (select y.{DbCacheValue.HashColumn} from {s}{CacheEntriesTableName} y
                                                               where y.{DbCacheValue.HashColumn} = {DbCacheValue.HashColumn}
                                                                 and y.{DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)}) x)
                      or {DbCacheEntry.ParentHash1Column} in (select x.{DbCacheValue.HashColumn} from
                                                             (select y.{DbCacheValue.HashColumn} from {s}{CacheEntriesTableName} y
                                                               where y.{DbCacheValue.HashColumn} = {DbCacheValue.HashColumn}
                                                                 and y.{DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)}) x)
                      or {DbCacheEntry.ParentHash2Column} in (select x.{DbCacheValue.HashColumn} from
                                                             (select y.{DbCacheValue.HashColumn} from {s}{CacheEntriesTableName} y
                                                               where y.{DbCacheValue.HashColumn} = {DbCacheValue.HashColumn}
                                                                 and y.{DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)}) x)))
            ");

            UpdateCacheEntryExpiryCommand = MinifyQuery($@"
                update {s}{CacheEntriesTableName}
                   set {DbCacheValue.UtcExpiryColumn} = {p}{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheEntry.Single.Hash)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {s}{CacheEntriesTableName}
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheEntry.Single.Hash)}
                   and {DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {s}{CacheEntriesTableName}
                 where ({p}{nameof(DbCacheEntry.Group.Hash)} is null or ({DbCacheValue.HashColumn} >= {p}{nameof(DbCacheEntry.Group.Hash)} and {DbCacheValue.HashColumn} <= {p}{nameof(DbCacheEntry.Group.Hash)} + {t}))
                   and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or {DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntriesQuery = MinifyQuery($@"
                select x.{DbCacheValue.HashColumn}        {l}{nameof(DbCacheValue.Hash)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r},
                       x.{DbCacheEntry.PartitionColumn}   {l}{nameof(DbCacheEntry.Partition)}{r},
                       x.{DbCacheEntry.KeyColumn}         {l}{nameof(DbCacheEntry.Key)}{r},
                       x.{DbCacheEntry.UtcCreationColumn} {l}{nameof(DbCacheEntry.UtcCreation)}{r},
                       x.{DbCacheEntry.ParentKey0Column}  {l}{nameof(DbCacheEntry.ParentKey0)}{r},
                       x.{DbCacheEntry.ParentKey1Column}  {l}{nameof(DbCacheEntry.ParentKey1)}{r},
                       x.{DbCacheEntry.ParentKey2Column}  {l}{nameof(DbCacheEntry.ParentKey2)}{r}
                  from {s}{CacheEntriesTableName} x
                 where ({p}{nameof(DbCacheEntry.Group.Hash)} is null or (x.{DbCacheValue.HashColumn} >= {p}{nameof(DbCacheEntry.Group.Hash)} and x.{DbCacheValue.HashColumn} <= {p}{nameof(DbCacheEntry.Group.Hash)} + {t}))
                   and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            // Partition and key are not "select-ed" because the caller already knows them.
            PeekCacheEntryQuery = MinifyQuery($@"
                select x.{DbCacheValue.HashColumn}        {l}{nameof(DbCacheValue.Hash)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r},
                       x.{DbCacheEntry.UtcCreationColumn} {l}{nameof(DbCacheEntry.UtcCreation)}{r},
                       x.{DbCacheEntry.ParentKey0Column}  {l}{nameof(DbCacheEntry.ParentKey0)}{r},
                       x.{DbCacheEntry.ParentKey1Column}  {l}{nameof(DbCacheEntry.ParentKey1)}{r},
                       x.{DbCacheEntry.ParentKey2Column}  {l}{nameof(DbCacheEntry.ParentKey2)}{r}
                  from {s}{CacheEntriesTableName} x
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheEntry.Single.Hash)}
                   and ({p}{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select x.{DbCacheValue.HashColumn}        {l}{nameof(DbCacheValue.Hash)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r}
                  from {s}{CacheEntriesTableName} x
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheEntry.Single.Hash)}
                   and ({p}{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            GetCacheSizeInBytesQuery = MinifyQuery($@"
                select sum({LengthSqlFunction}({DbCacheValue.ValueColumn}))
                     + sum({LengthSqlFunction}({DbCacheEntry.PartitionColumn}))
                     + sum({LengthSqlFunction}({DbCacheEntry.KeyColumn}))
                     + count(*) * (4*8) -- Four fields of 8 bytes: hash, expiry, interval, creation
                  from {s}{CacheEntriesTableName}
            ");

            #endregion Queries
        }

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
