// File name: SQLiteDbCacheConnectionFactory.cs
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

using Dapper;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace PommaLabs.KVLite.SQLite
{
    internal sealed class SQLiteCacheConnectionFactory<TSettings> : DbCacheConnectionFactory<SQLiteConnection>
        where TSettings : SQLiteCacheSettings<TSettings>
    {
        #region Constants

        /// <summary>
        ///   The default SQLite page size in bytes. Do not change this value unless SQLite changes
        ///   its defaults. WAL journal does limit the capability to change that value even when the
        ///   DB is still empty.
        /// </summary>
        private const int PageSizeInBytes = 4096;

        #endregion Constants

        private readonly TSettings _settings;
        private readonly SQLiteJournalModeEnum _journalMode;
        private string _vacuumCommand;
        private string _createCacheSchemaCommand;
        private string _getCacheEntriesSchemaQuery;

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

        public SQLiteCacheConnectionFactory(TSettings settings, SQLiteJournalModeEnum journalMode)
            : base(SQLiteFactory.Instance, null, null)
        {
            _settings = settings;
            _journalMode = journalMode;
        }

        /// <summary>
        ///   This method is called when either the cache schema name or the cache entries table name
        ///   have been changed by the user.
        /// </summary>
        protected override void UpdateCommandsAndQueries()
        {
            base.UpdateCommandsAndQueries();

            var p = ParameterPrefix;

            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert or ignore into {CacheEntriesTableName} (
                    {DbCacheEntry.PartitionColumn},
                    {DbCacheEntry.KeyColumn},
                    {DbCacheValue.UtcExpiryColumn},
                    {DbCacheValue.IntervalColumn},
                    {DbCacheValue.ValueColumn},
                    {DbCacheValue.CompressedColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentKey4Column}
                )
                values (
                    {p}{nameof(DbCacheEntry.Partition)},
                    {p}{nameof(DbCacheEntry.Key)},
                    {p}{nameof(DbCacheEntry.UtcExpiry)},
                    {p}{nameof(DbCacheEntry.Interval)},
                    {p}{nameof(DbCacheEntry.Value)},
                    {p}{nameof(DbCacheEntry.Compressed)},
                    {p}{nameof(DbCacheEntry.UtcCreation)},
                    {p}{nameof(DbCacheEntry.ParentKey0)},
                    {p}{nameof(DbCacheEntry.ParentKey1)},
                    {p}{nameof(DbCacheEntry.ParentKey2)},
                    {p}{nameof(DbCacheEntry.ParentKey3)},
                    {p}{nameof(DbCacheEntry.ParentKey4)}
                );

                update {CacheEntriesTableName}
                   set {DbCacheValue.UtcExpiryColumn} = {p}{nameof(DbCacheEntry.UtcExpiry)},
                       {DbCacheValue.IntervalColumn} = {p}{nameof(DbCacheEntry.Interval)},
                       {DbCacheValue.ValueColumn} = {p}{nameof(DbCacheEntry.Value)},
                       {DbCacheValue.CompressedColumn} = {p}{nameof(DbCacheEntry.Compressed)},
                       {DbCacheEntry.UtcCreationColumn} = {p}{nameof(DbCacheEntry.UtcCreation)},
                       {DbCacheEntry.ParentKey0Column} = {p}{nameof(DbCacheEntry.ParentKey0)},
                       {DbCacheEntry.ParentKey1Column} = {p}{nameof(DbCacheEntry.ParentKey1)},
                       {DbCacheEntry.ParentKey2Column} = {p}{nameof(DbCacheEntry.ParentKey2)},
                       {DbCacheEntry.ParentKey3Column} = {p}{nameof(DbCacheEntry.ParentKey3)},
                       {DbCacheEntry.ParentKey4Column} = {p}{nameof(DbCacheEntry.ParentKey4)}
                 where {DbCacheEntry.PartitionColumn} = {p}{nameof(DbCacheEntry.Partition)}
                   and {DbCacheEntry.KeyColumn} = {p}{nameof(DbCacheEntry.Key)}
                   and changes() = 0 -- Above INSERT has failed
            ");

            #endregion Commands

            #region Specific queries and commands

            _vacuumCommand = MinifyQuery($@"
                vacuum; -- Clears free list and makes DB file smaller
            ");

            _createCacheSchemaCommand = MinifyQuery($@"
                DROP TABLE IF EXISTS {CacheSchemaName}.{CacheEntriesTableName};
                CREATE TABLE {CacheSchemaName}.{CacheEntriesTableName} (
                    kvle_partition TEXT NOT NULL,
                    kvle_key TEXT NOT NULL,
                    kvle_expiry BIGINT NOT NULL,
                    kvle_interval BIGINT NOT NULL,
                    kvle_value BLOB NOT NULL,
                    kvle_compressed BOOLEAN NOT NULL,
                    kvle_creation BIGINT NOT NULL,
                    kvle_parent_key0 TEXT,
                    kvle_parent_key1 TEXT,
                    kvle_parent_key2 TEXT,
                    kvle_parent_key3 TEXT,
                    kvle_parent_key4 TEXT,
                    CONSTRAINT pk_kvle PRIMARY KEY (kvle_partition, kvle_key),
                    CONSTRAINT fk_kvle_parent0 FOREIGN KEY (kvle_partition, kvle_parent_key0) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                    CONSTRAINT fk_kvle_parent1 FOREIGN KEY (kvle_partition, kvle_parent_key1) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                    CONSTRAINT fk_kvle_parent2 FOREIGN KEY (kvle_partition, kvle_parent_key2) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                    CONSTRAINT fk_kvle_parent3 FOREIGN KEY (kvle_partition, kvle_parent_key3) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                    CONSTRAINT fk_kvle_parent4 FOREIGN KEY (kvle_partition, kvle_parent_key4) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE
                );
                CREATE INDEX ix_kvle_exp_part ON {CacheEntriesTableName} (kvle_expiry DESC, kvle_partition ASC);
                CREATE INDEX ix_kvle_parent0 ON {CacheEntriesTableName} (kvle_partition, kvle_parent_key0);
                CREATE INDEX ix_kvle_parent1 ON {CacheEntriesTableName} (kvle_partition, kvle_parent_key1);
                CREATE INDEX ix_kvle_parent2 ON {CacheEntriesTableName} (kvle_partition, kvle_parent_key2);
                CREATE INDEX ix_kvle_parent3 ON {CacheEntriesTableName} (kvle_partition, kvle_parent_key3);
                CREATE INDEX ix_kvle_parent4 ON {CacheEntriesTableName} (kvle_partition, kvle_parent_key4);
            ");

            _getCacheEntriesSchemaQuery = MinifyQuery($@"
                PRAGMA table_info({CacheEntriesTableName})
            ");

            #endregion Specific queries and commands
        }

        #region IDbCacheConnectionFactory members

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        public override string ConnectionString
        {
            get { return _connectionString; }
            set { throw new NotSupportedException(); }
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

        internal void InitConnectionString(string dataSource)
        {
            _connectionString = $"baseschemaname=kvlite;fulluri=\"{dataSource}\";journal mode={_journalMode};failifmissing=False;legacy format=False;read only=False;synchronous=Off;version=3;datetimeformat=Ticks;datetimekind=Utc;default timeout=180;prepareretries=3;enlist=False;foreign keys=True;recursive triggers=True;max page count={_settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes};page size={PageSizeInBytes};cache size=-2000;pooling=True";
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

            return columns.Count == 12
                && columns.Contains(DbCacheEntry.PartitionColumn)
                && columns.Contains(DbCacheEntry.KeyColumn)
                && columns.Contains(DbCacheValue.UtcExpiryColumn)
                && columns.Contains(DbCacheValue.IntervalColumn)
                && columns.Contains(DbCacheValue.ValueColumn)
                && columns.Contains(DbCacheValue.CompressedColumn)
                && columns.Contains(DbCacheEntry.UtcCreationColumn)
                && columns.Contains(DbCacheEntry.ParentKey0Column)
                && columns.Contains(DbCacheEntry.ParentKey1Column)
                && columns.Contains(DbCacheEntry.ParentKey2Column)
                && columns.Contains(DbCacheEntry.ParentKey3Column)
                && columns.Contains(DbCacheEntry.ParentKey4Column);
        }
    }
}
