// File name: SQLiteDbCacheConnectionFactory.cs
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

using Dapper;
using Microsoft.Data.Sqlite;
using PommaLabs.KVLite.Database;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.SQLite
{
    /// <summary>
    ///   Cache connection factory specialized for SQLite.
    /// </summary>
    public sealed class SQLiteCacheConnectionFactory<TSettings> : DbCacheConnectionFactory<TSettings, SqliteConnection>
        where TSettings : SQLiteCacheSettings<TSettings>
    {
        private readonly string _mode;
        private readonly string _pragmas;
        private string _vacuumCommand;
        private string _createCacheSchemaCommand;
        private string _getCacheEntriesSchemaQuery;

        /// <summary>
        ///   Cache connection factory specialized for SQLite.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="mode">Storage mode.</param>
        /// <param name="journal">Journal type.</param>
        public SQLiteCacheConnectionFactory(TSettings settings, string mode, string journal)
            : base(settings, SqliteFactory.Instance)
        {
            _mode = mode;

            // Used to configure each connection.
            _pragmas = MinifyQuery($@"
                PRAGMA foreign_keys = ON;
                PRAGMA journal_mode = {journal};
                PRAGMA synchronous = OFF;
            ");
        }

        /// <summary>
        ///   This method is called when either the cache schema name or the cache entries table name
        ///   have been changed by the user.
        /// </summary>
        protected override void UpdateCommandsAndQueries()
        {
            base.UpdateCommandsAndQueries();

            var p = ParameterPrefix;
            var s = SqlSchemaWithDot;

            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert or ignore into {s}{Settings.CacheEntriesTableName} (
                    {DbCacheValue.HashColumn},
                    {DbCacheValue.UtcExpiryColumn},
                    {DbCacheValue.IntervalColumn},
                    {DbCacheValue.ValueColumn},
                    {DbCacheValue.CompressedColumn},
                    {DbCacheEntry.PartitionColumn},
                    {DbCacheEntry.KeyColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentHash0Column},
                    {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column},
                    {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column},
                    {DbCacheEntry.ParentKey2Column}
                )
                values (
                    {p}{nameof(DbCacheValue.Hash)},
                    {p}{nameof(DbCacheValue.UtcExpiry)},
                    {p}{nameof(DbCacheValue.Interval)},
                    {p}{nameof(DbCacheValue.Value)},
                    {p}{nameof(DbCacheValue.Compressed)},
                    {p}{nameof(DbCacheEntry.Partition)},
                    {p}{nameof(DbCacheEntry.Key)},
                    {p}{nameof(DbCacheEntry.UtcCreation)},
                    {p}{nameof(DbCacheEntry.ParentHash0)},
                    {p}{nameof(DbCacheEntry.ParentKey0)},
                    {p}{nameof(DbCacheEntry.ParentHash1)},
                    {p}{nameof(DbCacheEntry.ParentKey1)},
                    {p}{nameof(DbCacheEntry.ParentHash2)},
                    {p}{nameof(DbCacheEntry.ParentKey2)}
                );

                update {s}{Settings.CacheEntriesTableName}
                   set {DbCacheValue.UtcExpiryColumn} = {p}{nameof(DbCacheValue.UtcExpiry)},
                       {DbCacheValue.IntervalColumn} = {p}{nameof(DbCacheValue.Interval)},
                       {DbCacheValue.ValueColumn} = {p}{nameof(DbCacheValue.Value)},
                       {DbCacheValue.CompressedColumn} = {p}{nameof(DbCacheValue.Compressed)},
                       {DbCacheEntry.UtcCreationColumn} = {p}{nameof(DbCacheEntry.UtcCreation)},
                       {DbCacheEntry.ParentHash0Column} = {p}{nameof(DbCacheEntry.ParentHash0)},
                       {DbCacheEntry.ParentKey0Column} = {p}{nameof(DbCacheEntry.ParentKey0)},
                       {DbCacheEntry.ParentHash1Column} = {p}{nameof(DbCacheEntry.ParentHash1)},
                       {DbCacheEntry.ParentKey1Column} = {p}{nameof(DbCacheEntry.ParentKey1)},
                       {DbCacheEntry.ParentHash2Column} = {p}{nameof(DbCacheEntry.ParentHash2)},
                       {DbCacheEntry.ParentKey2Column} = {p}{nameof(DbCacheEntry.ParentKey2)}
                 where {DbCacheValue.HashColumn} = {p}{nameof(DbCacheValue.Hash)}
                   and changes() = 0; -- Above INSERT has failed
            ");

            #endregion Commands

            #region Specific queries and commands

            _vacuumCommand = MinifyQuery($@"
                vacuum; -- Clears free list and makes DB file smaller
            ");

            _createCacheSchemaCommand = MinifyQuery($@"
                DROP TABLE IF EXISTS {s}{Settings.CacheEntriesTableName};
                CREATE TABLE {s}{Settings.CacheEntriesTableName} (
                    {DbCacheValue.IdColumn} INTEGER PRIMARY KEY,
                    {DbCacheValue.HashColumn} BIGINT NOT NULL,
                    {DbCacheValue.UtcExpiryColumn} BIGINT NOT NULL,
                    {DbCacheValue.IntervalColumn} BIGINT NOT NULL,
                    {DbCacheValue.ValueColumn} BLOB NOT NULL,
                    {DbCacheValue.CompressedColumn} BOOLEAN NOT NULL,
                    {DbCacheEntry.PartitionColumn} TEXT NOT NULL,
                    {DbCacheEntry.KeyColumn} TEXT NOT NULL,
                    {DbCacheEntry.UtcCreationColumn} BIGINT NOT NULL,
                    {DbCacheEntry.ParentHash0Column} BIGINT,
                    {DbCacheEntry.ParentKey0Column} TEXT,
                    {DbCacheEntry.ParentHash1Column} BIGINT,
                    {DbCacheEntry.ParentKey1Column} TEXT,
                    {DbCacheEntry.ParentHash2Column} BIGINT,
                    {DbCacheEntry.ParentKey2Column} TEXT,
                    CONSTRAINT uk_kvle UNIQUE ({DbCacheValue.HashColumn}),
                    FOREIGN KEY ({DbCacheEntry.ParentHash0Column}) REFERENCES {s}{Settings.CacheEntriesTableName} ({DbCacheValue.HashColumn}) ON DELETE CASCADE,
                    FOREIGN KEY ({DbCacheEntry.ParentHash1Column}) REFERENCES {s}{Settings.CacheEntriesTableName} ({DbCacheValue.HashColumn}) ON DELETE CASCADE,
                    FOREIGN KEY ({DbCacheEntry.ParentHash2Column}) REFERENCES {s}{Settings.CacheEntriesTableName} ({DbCacheValue.HashColumn}) ON DELETE CASCADE
                );
                CREATE INDEX ix_kvle_parent0 ON {s}{Settings.CacheEntriesTableName} ({DbCacheEntry.ParentHash0Column});
                CREATE INDEX ix_kvle_parent1 ON {s}{Settings.CacheEntriesTableName} ({DbCacheEntry.ParentHash1Column});
                CREATE INDEX ix_kvle_parent2 ON {s}{Settings.CacheEntriesTableName} ({DbCacheEntry.ParentHash2Column});
            ");

            _getCacheEntriesSchemaQuery = MinifyQuery($@"
                PRAGMA table_info({s}{Settings.CacheEntriesTableName})
            ");

            #endregion Specific queries and commands
        }

        #region IDbCacheConnectionFactory members

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <returns>An open connection.</returns>
        public override SqliteConnection Open()
        {
            var conn = base.Open();
            conn.Execute(_pragmas);
            return conn;
        }

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        public override async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
        {
            var conn = await base.OpenAsync(cancellationToken).ConfigureAwait(false);
            await conn.ExecuteAsync(_pragmas).ConfigureAwait(false);
            return conn;
        }

        #endregion IDbCacheConnectionFactory members

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            // Vacuum cannot be run within a transaction.
            using (var db = Open())
            {
                db.Execute(_vacuumCommand);
            }
        }

        internal string InitConnectionString(string dataSource)
        {
            return $"Data Source={dataSource};Mode={_mode};Cache=Shared";
        }

        internal void EnsureSchemaIsReady()
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                var isSchemaReady = true;

                cmd.CommandText = _getCacheEntriesSchemaQuery;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsCacheEntriesTableReady(dataReader);
                }

                if (!isSchemaReady)
                {
                    // Creates cache entries table and required indexes.
                    cmd.CommandText = _createCacheSchemaCommand;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool IsCacheEntriesTableReady(IDataReader dataReader)
        {
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 15
                && columns.Contains(DbCacheValue.IdColumn)
                && columns.Contains(DbCacheValue.HashColumn)
                && columns.Contains(DbCacheValue.UtcExpiryColumn)
                && columns.Contains(DbCacheValue.IntervalColumn)
                && columns.Contains(DbCacheValue.ValueColumn)
                && columns.Contains(DbCacheValue.CompressedColumn)
                && columns.Contains(DbCacheEntry.PartitionColumn)
                && columns.Contains(DbCacheEntry.KeyColumn)
                && columns.Contains(DbCacheEntry.UtcCreationColumn)
                && columns.Contains(DbCacheEntry.ParentHash0Column)
                && columns.Contains(DbCacheEntry.ParentKey0Column)
                && columns.Contains(DbCacheEntry.ParentHash1Column)
                && columns.Contains(DbCacheEntry.ParentKey1Column)
                && columns.Contains(DbCacheEntry.ParentHash2Column)
                && columns.Contains(DbCacheEntry.ParentKey2Column);
        }
    }
}
