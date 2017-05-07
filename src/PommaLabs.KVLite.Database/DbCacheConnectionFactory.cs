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

using PommaLabs.Thrower;
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
        public static string DefaultCacheSchemaName { get; } = "kvlite";

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

            #region Commands

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {s}{CacheEntriesTableName}
                 where {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)}
                   and ({DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}
                     or {DbCacheValue.KeyColumn} = (select y.{DbCacheEntry.ParentKey0Column} from {s}{CacheEntriesTableName} y where y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)} and y.{DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)})
                     or {DbCacheValue.KeyColumn} = (select y.{DbCacheEntry.ParentKey1Column} from {s}{CacheEntriesTableName} y where y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)} and y.{DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)})
                     or {DbCacheValue.KeyColumn} = (select y.{DbCacheEntry.ParentKey2Column} from {s}{CacheEntriesTableName} y where y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)} and y.{DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)})
                     or {DbCacheValue.KeyColumn} = (select y.{DbCacheEntry.ParentKey3Column} from {s}{CacheEntriesTableName} y where y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)} and y.{DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)})
                     or {DbCacheValue.KeyColumn} = (select y.{DbCacheEntry.ParentKey4Column} from {s}{CacheEntriesTableName} y where y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)} and y.{DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}))
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {s}{CacheEntriesTableName}
                 where (({p}{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Group.Partition)})
                    and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or {DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)}))
                    or exists (select 1 from {s}{CacheEntriesTableName} y
                                where ({p}{nameof(DbCacheEntry.Group.Partition)} is null or y.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Group.Partition)})
                                  and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or y.{DbCacheValue.UtcExpiryColumn} < {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
                                  and y.{DbCacheValue.PartitionColumn} = {DbCacheValue.PartitionColumn}
                                  and y.{DbCacheValue.KeyColumn} = {DbCacheEntry.ParentKey0Column})
            ");

            UpdateCacheEntryExpiryCommand = MinifyQuery($@"
                update {s}{CacheEntriesTableName}
                   set {DbCacheValue.UtcExpiryColumn} = {p}{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {s}{CacheEntriesTableName}
                 where {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}
                   and {DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {s}{CacheEntriesTableName}
                 where ({p}{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Group.Partition)})
                   and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or {DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntriesQuery = MinifyQuery($@"
                select x.{DbCacheValue.PartitionColumn}   {l}{nameof(DbCacheValue.Partition)}{r},
                       x.{DbCacheValue.KeyColumn}         {l}{nameof(DbCacheValue.Key)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r},
                       x.{DbCacheValue.UtcCreationColumn} {l}{nameof(DbCacheValue.UtcCreation)}{r},
                       x.{DbCacheEntry.ParentKey0Column}  {l}{nameof(DbCacheEntry.ParentKey0)}{r},
                       x.{DbCacheEntry.ParentKey1Column}  {l}{nameof(DbCacheEntry.ParentKey1)}{r},
                       x.{DbCacheEntry.ParentKey2Column}  {l}{nameof(DbCacheEntry.ParentKey2)}{r},
                       x.{DbCacheEntry.ParentKey3Column}  {l}{nameof(DbCacheEntry.ParentKey3)}{r},
                       x.{DbCacheEntry.ParentKey4Column}  {l}{nameof(DbCacheEntry.ParentKey4)}{r}
                  from {s}{CacheEntriesTableName} x
                 where ({p}{nameof(DbCacheEntry.Group.Partition)} is null or x.{DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Group.Partition)})
                   and ({p}{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntryQuery = MinifyQuery($@"
                select x.{DbCacheValue.PartitionColumn}   {l}{nameof(DbCacheValue.Partition)}{r},
                       x.{DbCacheValue.KeyColumn}         {l}{nameof(DbCacheValue.Key)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r},
                       x.{DbCacheValue.UtcCreationColumn} {l}{nameof(DbCacheValue.UtcCreation)}{r},
                       x.{DbCacheEntry.ParentKey0Column}  {l}{nameof(DbCacheEntry.ParentKey0)}{r},
                       x.{DbCacheEntry.ParentKey1Column}  {l}{nameof(DbCacheEntry.ParentKey1)}{r},
                       x.{DbCacheEntry.ParentKey2Column}  {l}{nameof(DbCacheEntry.ParentKey2)}{r},
                       x.{DbCacheEntry.ParentKey3Column}  {l}{nameof(DbCacheEntry.ParentKey3)}{r},
                       x.{DbCacheEntry.ParentKey4Column}  {l}{nameof(DbCacheEntry.ParentKey4)}{r}
                  from {s}{CacheEntriesTableName} x
                 where {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}
                   and ({p}{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select x.{DbCacheValue.PartitionColumn}   {l}{nameof(DbCacheValue.Partition)}{r},
                       x.{DbCacheValue.KeyColumn}         {l}{nameof(DbCacheValue.Key)}{r},
                       x.{DbCacheValue.UtcExpiryColumn}   {l}{nameof(DbCacheValue.UtcExpiry)}{r},
                       x.{DbCacheValue.IntervalColumn}    {l}{nameof(DbCacheValue.Interval)}{r},
                       x.{DbCacheValue.ValueColumn}       {l}{nameof(DbCacheValue.Value)}{r},
                       x.{DbCacheValue.CompressedColumn}  {l}{nameof(DbCacheValue.Compressed)}{r},
                       x.{DbCacheValue.UtcCreationColumn} {l}{nameof(DbCacheValue.UtcCreation)}{r}
                  from {s}{CacheEntriesTableName} x
                 where {DbCacheValue.PartitionColumn} = {p}{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheValue.KeyColumn} = {p}{nameof(DbCacheEntry.Single.Key)}
                   and ({p}{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} = 1 or x.{DbCacheValue.UtcExpiryColumn} >= {p}{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            GetCacheSizeInBytesQuery = MinifyQuery($@"
                select sum({LengthSqlFunction}({DbCacheValue.PartitionColumn}))
                     + sum({LengthSqlFunction}({DbCacheValue.KeyColumn}))
                     + sum({LengthSqlFunction}({DbCacheValue.ValueColumn}))
                     + count(*) * (3*8) -- Three fields of 8 bytes: expiry, interval, creation
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
