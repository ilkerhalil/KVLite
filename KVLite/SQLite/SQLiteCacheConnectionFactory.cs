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

using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;

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

        private static readonly DbProviderFactory DbProviderFactory;

        static SQLiteCacheConnectionFactory()
        {
            try
            {
                var factoryType = Type.GetType("System.Data.SQLite.SQLiteFactory, System.Data.SQLite");
                DbProviderFactory = factoryType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null) as DbProviderFactory;
            }
            catch (Exception ex)
            {
                throw new Exception(ErrorMessages.MissingSQLiteDriver, ex);
            }
        }

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

        private readonly TSettings _settings;

        private readonly SQLiteJournalMode _journalMode;

        public SQLiteCacheConnectionFactory(TSettings settings, SQLiteJournalMode journalMode)
            : base(DbProviderFactory, null, null, null)
        {
            _settings = settings;
            _journalMode = journalMode;

            #region Commands

            InsertOrUpdateCacheItemCommand = MinifyQuery($@"
                insert or ignore into {CacheItemsTableName} (
                    {DbCacheEntry.HashColumn}, {DbCacheEntry.UtcExpiryColumn}, {DbCacheEntry.IntervalColumn},
                    {DbCacheEntry.ValueColumn}, {DbCacheEntry.CompressedColumn},
                    {DbCacheEntry.PartitionColumn}, {DbCacheEntry.KeyColumn}, {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentHash0Column}, {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column}, {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column}, {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentHash3Column}, {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentHash4Column}, {DbCacheEntry.ParentKey4Column}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)}, @{nameof(DbCacheEntry.UtcExpiry)}, @{nameof(DbCacheEntry.Interval)},
                    @{nameof(DbCacheEntry.Value)}, @{nameof(DbCacheEntry.Compressed)},
                    @{nameof(DbCacheEntry.Partition)}, @{nameof(DbCacheEntry.Key)}, @{nameof(DbCacheEntry.UtcCreation)},
                    @{nameof(DbCacheEntry.ParentHash0)}, @{nameof(DbCacheEntry.ParentKey0)},
                    @{nameof(DbCacheEntry.ParentHash1)}, @{nameof(DbCacheEntry.ParentKey1)},
                    @{nameof(DbCacheEntry.ParentHash2)}, @{nameof(DbCacheEntry.ParentKey2)},
                    @{nameof(DbCacheEntry.ParentHash3)}, @{nameof(DbCacheEntry.ParentKey3)},
                    @{nameof(DbCacheEntry.ParentHash4)}, @{nameof(DbCacheEntry.ParentKey4)}
                );

                update {CacheItemsTableName}
                   set {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.UtcExpiry)},
                       {DbCacheEntry.IntervalColumn} = @{nameof(DbCacheEntry.Interval)},
                       {DbCacheEntry.ValueColumn} = @{nameof(DbCacheEntry.Value)},
                       {DbCacheEntry.CompressedColumn} = @{nameof(DbCacheEntry.Compressed)},
                       {DbCacheEntry.UtcCreationColumn} = @{nameof(DbCacheEntry.UtcCreation)},
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
                   and changes() = 0 -- Above INSERT has failed
            ");

            DeleteCacheItemCommand = MinifyQuery($@"
                delete from {CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            DeleteCacheItemsCommand = MinifyQuery($@"
                delete from {CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            UpdateCacheItemExpiryCommand = MinifyQuery($@"
                update {CacheItemsTableName}
                   set {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheItemQuery = MinifyQuery($@"
                select count(*)
                  from {CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheItemsQuery = MinifyQuery($@"
                select count(*)
                  from {CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheItemsQuery = MinifyQuery($@"
                select x.{DbCacheEntry.UtcExpiryColumn} {nameof(DbCacheEntry.UtcExpiry)},
                       x.{DbCacheEntry.IntervalColumn} {nameof(DbCacheEntry.Interval)},
                       x.{DbCacheEntry.ValueColumn} {nameof(DbCacheEntry.Value)},
                       x.{DbCacheEntry.CompressedColumn} {nameof(DbCacheEntry.Compressed)},
                       x.{DbCacheEntry.PartitionColumn} {nameof(DbCacheEntry.Partition)},
                       x.{DbCacheEntry.KeyColumn} {nameof(DbCacheEntry.Key)},
                       x.{DbCacheEntry.UtcCreationColumn} {nameof(DbCacheEntry.UtcCreation)},
                       x.{DbCacheEntry.ParentHash0Column} {nameof(DbCacheEntry.ParentHash0)},
                       x.{DbCacheEntry.ParentKey0Column} {nameof(DbCacheEntry.ParentKey0)},
                       x.{DbCacheEntry.ParentHash1Column} {nameof(DbCacheEntry.ParentHash1)},
                       x.{DbCacheEntry.ParentKey1Column} {nameof(DbCacheEntry.ParentKey1)},
                       x.{DbCacheEntry.ParentHash2Column} {nameof(DbCacheEntry.ParentHash2)},
                       x.{DbCacheEntry.ParentKey2Column} {nameof(DbCacheEntry.ParentKey2)},
                       x.{DbCacheEntry.ParentHash3Column} {nameof(DbCacheEntry.ParentHash3)},
                       x.{DbCacheEntry.ParentKey3Column} {nameof(DbCacheEntry.ParentKey3)},
                       x.{DbCacheEntry.ParentHash4Column} {nameof(DbCacheEntry.ParentHash4)},
                       x.{DbCacheEntry.ParentKey4Column} {nameof(DbCacheEntry.ParentKey4)}
                  from {CacheItemsTableName} x
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or x.{DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or x.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheItemQuery = MinifyQuery($@"
                select x.{DbCacheEntry.UtcExpiryColumn} {nameof(DbCacheEntry.UtcExpiry)},
                       x.{DbCacheEntry.IntervalColumn} {nameof(DbCacheEntry.Interval)},
                       x.{DbCacheEntry.ValueColumn} {nameof(DbCacheEntry.Value)},
                       x.{DbCacheEntry.CompressedColumn} {nameof(DbCacheEntry.Compressed)},
                       x.{DbCacheEntry.PartitionColumn} {nameof(DbCacheEntry.Partition)},
                       x.{DbCacheEntry.KeyColumn} {nameof(DbCacheEntry.Key)},
                       x.{DbCacheEntry.UtcCreationColumn} {nameof(DbCacheEntry.UtcCreation)},
                       x.{DbCacheEntry.ParentHash0Column} {nameof(DbCacheEntry.ParentHash0)},
                       x.{DbCacheEntry.ParentKey0Column} {nameof(DbCacheEntry.ParentKey0)},
                       x.{DbCacheEntry.ParentHash1Column} {nameof(DbCacheEntry.ParentHash1)},
                       x.{DbCacheEntry.ParentKey1Column} {nameof(DbCacheEntry.ParentKey1)},
                       x.{DbCacheEntry.ParentHash2Column} {nameof(DbCacheEntry.ParentHash2)},
                       x.{DbCacheEntry.ParentKey2Column} {nameof(DbCacheEntry.ParentKey2)},
                       x.{DbCacheEntry.ParentHash3Column} {nameof(DbCacheEntry.ParentHash3)},
                       x.{DbCacheEntry.ParentKey3Column} {nameof(DbCacheEntry.ParentKey3)},
                       x.{DbCacheEntry.ParentHash4Column} {nameof(DbCacheEntry.ParentHash4)},
                       x.{DbCacheEntry.ParentKey4Column} {nameof(DbCacheEntry.ParentKey4)}
                  from {CacheItemsTableName} x
                 where x.{DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or x.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select y.{nameof(DbCacheEntry.UtcExpiry)} {nameof(DbCacheValue.UtcExpiry)},
                       y.{nameof(DbCacheEntry.Interval)} {nameof(DbCacheValue.Interval)},
                       y.{nameof(DbCacheEntry.Value)} {nameof(DbCacheValue.Value)},
                       y.{nameof(DbCacheEntry.Compressed)} {nameof(DbCacheValue.Compressed)}
                  from ({PeekCacheItemQuery}) y
            ");

            GetCacheSizeInBytesQuery = MinifyQuery($@"
                select round(sum(length({DbCacheEntry.ValueColumn})))
                  from {CacheSchemaName}.{CacheItemsTableName};
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
            _connectionString = $"baseschemaname=kvlite;fulluri=\"{dataSource}\";journal mode={_journalMode};failifmissing=False;legacy format=False;read only=False;synchronous=Off;version=3;datetimeformat=Ticks;datetimekind=Utc;default timeout=180;prepareretries=3;enlist=False;foreign keys=True;recursive triggers=True;max page count={_settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes};page size={PageSizeInBytes};cache size=-2000;pooling=True";
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
                    isSchemaReady = IsCacheItemsTableReady(dataReader);
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
            var columns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 18
                && columns.Contains(DbCacheEntry.HashColumn)
                && columns.Contains(DbCacheEntry.UtcExpiryColumn)
                && columns.Contains(DbCacheEntry.IntervalColumn)
                && columns.Contains(DbCacheEntry.ValueColumn)
                && columns.Contains(DbCacheEntry.CompressedColumn)
                && columns.Contains(DbCacheEntry.PartitionColumn)
                && columns.Contains(DbCacheEntry.KeyColumn)
                && columns.Contains(DbCacheEntry.UtcCreationColumn)
                && columns.Contains(DbCacheEntry.ParentHash0Column)
                && columns.Contains(DbCacheEntry.ParentKey0Column)
                && columns.Contains(DbCacheEntry.ParentHash1Column)
                && columns.Contains(DbCacheEntry.ParentKey1Column)
                && columns.Contains(DbCacheEntry.ParentHash2Column)
                && columns.Contains(DbCacheEntry.ParentKey2Column)
                && columns.Contains(DbCacheEntry.ParentHash3Column)
                && columns.Contains(DbCacheEntry.ParentKey3Column)
                && columns.Contains(DbCacheEntry.ParentHash4Column)
                && columns.Contains(DbCacheEntry.ParentKey4Column);
        }
    }

    internal enum SQLiteJournalMode
    {
        Default = -1,
        Delete = 0,
        Persist = 1,
        Off = 2,
        Truncate = 3,
        Memory = 4,
        Wal = 5
    }
}