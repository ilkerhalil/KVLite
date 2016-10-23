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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace PommaLabs.KVLite.SQLite
{
    internal sealed class SQLiteCacheConnectionFactory<TSettings> : DbCacheConnectionFactory
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

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

        private readonly TSettings _settings;

        private readonly SQLiteJournalModeEnum _journalMode;

        public SQLiteCacheConnectionFactory(TSettings settings, SQLiteJournalModeEnum journalMode)
            : base(SQLiteFactory.Instance, null, null, null)
        {
            _settings = settings;
            _journalMode = journalMode;

            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert or ignore into {CacheItemsTableName} (
                    {DbCacheEntry.HashColumn},
                    {DbCacheEntry.PartitionColumn}, {DbCacheEntry.KeyColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.UtcExpiryColumn}, {DbCacheEntry.IntervalColumn},
                    {DbCacheEntry.ParentHash0Column}, {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column}, {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column}, {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentHash3Column}, {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentHash4Column}, {DbCacheEntry.ParentKey4Column}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)},
                    @{nameof(DbCacheEntry.Partition)}, @{nameof(DbCacheEntry.Key)},
                    @{nameof(DbCacheEntry.UtcCreation)},
                    @{nameof(DbCacheEntry.UtcExpiry)}, @{nameof(DbCacheEntry.Interval)},
                    @{nameof(DbCacheEntry.ParentHash0)}, @{nameof(DbCacheEntry.ParentKey0)},
                    @{nameof(DbCacheEntry.ParentHash1)}, @{nameof(DbCacheEntry.ParentKey1)},
                    @{nameof(DbCacheEntry.ParentHash2)}, @{nameof(DbCacheEntry.ParentKey2)},
                    @{nameof(DbCacheEntry.ParentHash3)}, @{nameof(DbCacheEntry.ParentKey3)},
                    @{nameof(DbCacheEntry.ParentHash4)}, @{nameof(DbCacheEntry.ParentKey4)}
                );

                update {CacheItemsTableName}
                   set {DbCacheEntry.UtcCreationColumn} = @{nameof(DbCacheEntry.UtcCreation)},
                       {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.UtcExpiry)},
                       {DbCacheEntry.IntervalColumn} = @{nameof(DbCacheEntry.Interval)},
                       {DbCacheEntry.ParentHash0Column} = @{nameof(DbCacheEntry.ParentHash0)},
                       {DbCacheEntry.ParentKey0Column} = @{nameof(DbCacheEntry.ParentKey0)},
                       {DbCacheEntry.ParentHash1Column} = @{nameof(DbCacheEntry.ParentHash1)},
                       {DbCacheEntry.ParentKey1Column} = @{nameof(DbCacheEntry.ParentKey1)},
                       {DbCacheEntry.ParentHash2Column} = @{nameof(DbCacheEntry.ParentHash2)},
                       {DbCacheEntry.ParentKey2Column} = @{nameof(DbCacheEntry.ParentKey2)},
                       {DbCacheEntry.ParentHash3Column} = @{nameof(DbCacheEntry.ParentHash3)},
                       {DbCacheEntry.ParentKey3Column} = @{nameof(DbCacheEntry.ParentKey3)},
                       {DbCacheEntry.ParentHash4Column} = @{nameof(DbCacheEntry.ParentHash4)},
                       {DbCacheEntry.ParentKey4Column} = @{nameof(DbCacheEntry.ParentKey4)}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Hash)}
                   and changes() = 0; -- Above INSERT has failed

                insert or ignore into {CacheValuesTableName} (
                    {DbCacheEntry.HashColumn},
                    {DbCacheEntry.ValueColumn},
                    {DbCacheEntry.CompressedColumn}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)},
                    @{nameof(DbCacheEntry.Value)},
                    @{nameof(DbCacheEntry.Compressed)}
                );

                update {CacheValuesTableName}
                   set {DbCacheEntry.ValueColumn} = @{nameof(DbCacheEntry.Value)},
                       {DbCacheEntry.CompressedColumn} = @{nameof(DbCacheEntry.Compressed)}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Hash)}
                   and changes() = 0; -- Above INSERT has failed
            ");

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            UpdateCacheEntryExpiryCommand = MinifyQuery($@"
                update {CacheItemsTableName}
                   set {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntryQuery = MinifyQuery($@"
                select item.{DbCacheEntry.PartitionColumn} {nameof(DbCacheEntry.Partition)},
                       item.{DbCacheEntry.KeyColumn} {nameof(DbCacheEntry.Key)},
                       item.{DbCacheEntry.UtcCreationColumn} {nameof(DbCacheEntry.UtcCreation)},
                       item.{DbCacheEntry.UtcExpiryColumn} {nameof(DbCacheEntry.UtcExpiry)},
                       item.{DbCacheEntry.IntervalColumn} {nameof(DbCacheEntry.Interval)},
                       item.{DbCacheEntry.ParentHash0Column} {nameof(DbCacheEntry.ParentHash0)},
                       item.{DbCacheEntry.ParentKey0Column} {nameof(DbCacheEntry.ParentKey0)},
                       item.{DbCacheEntry.ParentHash1Column} {nameof(DbCacheEntry.ParentHash1)},
                       item.{DbCacheEntry.ParentKey1Column} {nameof(DbCacheEntry.ParentKey1)},
                       item.{DbCacheEntry.ParentHash2Column} {nameof(DbCacheEntry.ParentHash2)},
                       item.{DbCacheEntry.ParentKey2Column} {nameof(DbCacheEntry.ParentKey2)},
                       item.{DbCacheEntry.ParentHash3Column} {nameof(DbCacheEntry.ParentHash3)},
                       item.{DbCacheEntry.ParentKey3Column} {nameof(DbCacheEntry.ParentKey3)},
                       item.{DbCacheEntry.ParentHash4Column} {nameof(DbCacheEntry.ParentHash4)},
                       item.{DbCacheEntry.ParentKey4Column} {nameof(DbCacheEntry.ParentKey4)},
                       value.{DbCacheEntry.ValueColumn} {nameof(DbCacheEntry.Value)},
                       value.{DbCacheEntry.CompressedColumn} {nameof(DbCacheEntry.Compressed)}
                  from {CacheItemsTableName} item natural join {CacheValuesTableName} value
                 where item.{DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or item.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select entry.{nameof(DbCacheEntry.UtcExpiry)} {nameof(Core.DbCacheValue.UtcExpiry)},
                       entry.{nameof(DbCacheEntry.Interval)} {nameof(Core.DbCacheValue.Interval)},
                       entry.{nameof(DbCacheEntry.Value)} {nameof(Core.DbCacheValue.Value)},
                       entry.{nameof(DbCacheEntry.Compressed)} {nameof(Core.DbCacheValue.Compressed)}
                  from ({PeekCacheEntryQuery}) entry
            ");

            #endregion Queries
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

        public override long GetCacheSizeInBytes()
        {
            // No need for a transaction, since it is just a select.
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_count;";
                var pageCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA freelist_count;";
                var freelistCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA page_size;";
                var pageSizeInKB = (long) cmd.ExecuteScalar();

                return (pageCount - freelistCount) * pageSizeInKB;
            }
        }

        #endregion IDbCacheConnectionFactory members

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            // Vacuum cannot be run within a transaction.
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Vacuum;
                cmd.ExecuteNonQuery();
            }
        }

        internal void InitConnectionString(string dataSource)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = CacheSchemaName,
                FullUri = dataSource,
                JournalMode = _journalMode,
                FailIfMissing = false,
                LegacyFormat = false,
                ReadOnly = false,
                SyncMode = SynchronizationModes.Off,
                Version = 3,

                /* KVLite uses UNIX time */
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,

                /* Settings three minutes as timeout should be more than enough... */
                DefaultTimeout = 180,
                PrepareRetries = 3,

                /* Transaction handling */
                Enlist = false,

                /* Required by parent keys */
                ForeignKeys = true,
                RecursiveTriggers = true,

                /* Each page is 4KB large - Multiply by 1024*1024/PageSizeInBytes */
                MaxPageCount = _settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes,
                PageSize = PageSizeInBytes,
                CacheSize = -2000,

                /* We need pooling to improve performance */
                Pooling = true,
            };

            _connectionString = builder.ToString();
        }

        internal void EnsureSchemaIsReady()
        {
            using (var conn = Open())
            using (var cmd = conn.CreateCommand())
            {
                var isSchemaReady = true;

                cmd.CommandText = SQLiteQueries.IsCacheItemsTableReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady &= IsCacheItemsTableReady(dataReader);
                }

                cmd.CommandText = SQLiteQueries.IsCacheValuesTableReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady &= IsCacheValuesTableReady(dataReader);
                }

                if (!isSchemaReady)
                {
                    // Creates cache tables and required indexes.
                    cmd.CommandText = SQLiteQueries.CacheSchema;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool IsCacheItemsTableReady(IDataReader dataReader)
        {
            var columns = new HashSet<string>();

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 16
                && columns.Contains("kvli_hash")
                && columns.Contains("kvli_partition")
                && columns.Contains("kvli_key")
                && columns.Contains("kvli_creation")
                && columns.Contains("kvli_expiry")
                && columns.Contains("kvli_interval")
                && columns.Contains("kvli_parent_hash0")
                && columns.Contains("kvli_parent_key0")
                && columns.Contains("kvli_parent_hash1")
                && columns.Contains("kvli_parent_key1")
                && columns.Contains("kvli_parent_hash2")
                && columns.Contains("kvli_parent_key2")
                && columns.Contains("kvli_parent_hash3")
                && columns.Contains("kvli_parent_key3")
                && columns.Contains("kvli_parent_hash4")
                && columns.Contains("kvli_parent_key4");
        }

        private static bool IsCacheValuesTableReady(IDataReader dataReader)
        {
            var columns = new HashSet<string>();

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 3
                && columns.Contains("kvli_hash")
                && columns.Contains("kvlv_value")
                && columns.Contains("kvlv_compressed");
        }
    }
}