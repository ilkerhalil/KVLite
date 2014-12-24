﻿// File name: PersistentCache.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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

using CodeProject.ObjectPool;
using Dapper;
using PommaLabs.GRAMPA;
using PommaLabs.GRAMPA.Extensions;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class PersistentCache : CacheBase<PersistentCache, PersistentCacheSettings>
    {
        #region Constants

        private const int PageSizeInBytes = 32768;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        #endregion Constants

        private ObjectPool<PooledObjectWrapper<SQLiteConnection>> _connectionPool;
        private string _connectionString;

        /// <summary>
        ///   This value is increased for each ADD operation; after this value reaches the
        ///   "InsertionCountBeforeCleanup" configuration parameter, then we must reset it and do a
        ///   SOFT cleanup.
        /// </summary>
        private short _insertionCount;

        #region Construction

        static PersistentCache()
        {
            // Make SQLite work... (loading dll from e.g. KVLite/x64/SQLite.Interop.dll)
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", nativePath);
        }

        /// <summary>
        ///   TODO
        /// </summary>
        public PersistentCache()
            : this(new PersistentCacheSettings())
        {
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="settings"></param>
        public PersistentCache(PersistentCacheSettings settings)
            : base(settings)
        {
            settings.PropertyChanged += Settings_PropertyChanged;

            // If the directory which should contain the cache does not exist, then we create it.
            // SQLite will take care of creating the DB itself.
            var cacheDir = Path.GetDirectoryName(settings.CacheFile.MapPath());
            if (cacheDir != null && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            // ...
            InitConnectionString();

            using (var ctx = _connectionPool.GetObject())
            {
                using (var trx = ctx.InternalResource.BeginTransaction())
                {
                    if (ctx.InternalResource.ExecuteScalar<long>(SQLiteQueries.IsSchemaReady, trx) == 0)
                    {
                        // Creates the CacheItem table and the required indexes.
                        ctx.InternalResource.Execute(SQLiteQueries.CacheSchema, null, trx);
                    }
                    trx.Commit();
                }
            }

            // Initial cleanup
            Clear(CacheReadMode.ConsiderExpiryDate);
        }

        #endregion Construction

        #region Public Methods

        /// <summary>
        ///   </summary>
        /// <returns></returns>
        [Pure]
        private IList<object> PeekAll()
        {
            var p = new DynamicParameters();
            p.Add("partition", null, DbType.String);
            p.Add("key", null, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.Peek, p).Select(i => ToCacheItem(i).Value).Where(i => i != null).ToList();
            }
        }

        /// <summary>
        ///   TODO
        /// </summary>
        public void Vacuum()
        {
            // Vacuum cannot be run within a transaction.
            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Vacuum);
            }
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        public Task VacuumAsync()
        {
            return Task.Factory.StartNew(Vacuum);
        }

        #endregion Public Methods

        public override CacheKind Kind
        {
            get { return CacheKind.Persistent; }
        }

        public override void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, DateTime.UtcNow + interval, interval);
        }

        public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        public override void Clear()
        {
            Clear(CacheReadMode.ConsiderExpiryDate);
        }

        public void Clear(CacheReadMode cacheReadMode)
        {
            var p = new DynamicParameters();
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Clear, p);
            }
        }

        public override bool Contains(string partition, string key)
        {
            return Get(partition, key) != null;
        }

        public override long LongCount()
        {
            return LongCount(CacheReadMode.ConsiderExpiryDate);
        }

        public long LongCount(CacheReadMode cacheReadMode)
        {
            var p = new DynamicParameters();
            p.Add("ignoreExpirationDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);

            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<long>(SQLiteQueries.Count, p);
            }
        }

        public override CacheItem GetItem(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                var dbItem = ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.Get, p).FirstOrDefault();
                return (dbItem == null) ? null : ToCacheItem(dbItem);
            }
        }

        public override void Remove(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Remove, p);
            }
        }

        protected override IEnumerable<CacheItem> DoGetAllItems()
        {
            var p = new DynamicParameters();
            p.Add("partition", null, DbType.String);
            p.Add("key", null, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.Get, p).Select(ToCacheItem).Where(i => i != null);
            }
        }

        protected override IEnumerable<CacheItem> DoGetPartitionItems(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", null, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.Get, p).Select(ToCacheItem).Where(i => i != null);
            }
        }

        #region Private Methods

        private PooledObjectWrapper<SQLiteConnection> CreatePooledConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
            var walAutoCheckpointInPages = journalSizeLimitInBytes / PageSizeInBytes / 3;
            var pragmas = String.Format(SQLiteQueries.SetPragmas, journalSizeLimitInBytes, walAutoCheckpointInPages);
            connection.Execute(pragmas);
            return new PooledObjectWrapper<SQLiteConnection>(connection);
        }

        private void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            // Serializing may be pretty expensive, therefore we keep it out of the transaction.
            byte[] serializedValue;
            try
            {
                serializedValue = BinarySerializer.SerializeObject(value);
            }
            catch (Exception ex)
            {
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }
            var dbItem = new DbCacheItem
            {
                Partition = partition,
                Key = key,
                SerializedValue = serializedValue,
                UtcExpiry = utcExpiry.HasValue ? (long)(utcExpiry.Value - UnixEpoch).TotalSeconds : new long?(),
                Interval = interval.HasValue ? (long)interval.Value.TotalSeconds : new long?()
            };

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Add, dbItem);
            }

            // Insertion has concluded successfully, therefore we increment the operation counter.
            // If it has reached the "InsertionCountBeforeCleanup" configuration parameter, then we
            // must reset it and do a SOFT cleanup. Following code is not fully thread safe, but it
            // does not matter, because the "InsertionCountBeforeCleanup" parameter should be just
            // an hint on when to do the cleanup.
            _insertionCount++;
            if (_insertionCount >= Settings.InsertionCountBeforeCleanup)
            {
                _insertionCount = 0;
                Task.Factory.StartNew(() => Clear(CacheReadMode.ConsiderExpiryDate));
            }
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "CacheFile")
            {
                InitConnectionString();
            }
        }

        private void InitConnectionString()
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                BinaryGUID = true,
                BrowsableConnectionString = false,
                /* Number of pages of 32KB */
                CacheSize = 128,
                DataSource = Settings.CacheFile.MapPath(),
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,
                /* Settings ten minutes as timeout should be more than enough... */
                DefaultTimeout = 600,
                Enlist = false,
                FailIfMissing = false,
                ForeignKeys = false,
                JournalMode = SQLiteJournalModeEnum.Wal,
                LegacyFormat = false,
                MaxPageCount = Settings.MaxCacheSizeInMB * 32, // Each page is 32KB large - Multiply by 1024*1024/32768
                PageSize = PageSizeInBytes,
                /* We use a custom object pool */
                Pooling = false,
                ReadOnly = false,
                SyncMode = SynchronizationModes.Off,
                Version = 3,
            };

            _connectionString = builder.ToString();
            _connectionPool = new ObjectPool<PooledObjectWrapper<SQLiteConnection>>(1, 10, CreatePooledConnection);
        }

        private static CacheItem ToCacheItem(DbCacheItem original)
        {
            object deserializedValue;
            try
            {
                deserializedValue = BinarySerializer.DeserializeObject(original.SerializedValue);
            }
            catch
            {
                // Something wrong happened during deserialization. Therefore, we act as if there
                // was no element.
                return null;
            }
            return new CacheItem
            {
                Partition = original.Partition,
                Key = original.Key,
                Value = deserializedValue,
                UtcCreation = UnixEpoch.AddSeconds(original.UtcCreation),
                UtcExpiry = original.UtcExpiry == null ? new DateTime?() : UnixEpoch.AddSeconds(original.UtcExpiry.Value),
                Interval = original.Interval == null ? new TimeSpan?() : TimeSpan.FromSeconds(original.Interval.Value)
            };
        }

        #endregion Private Methods

        #region Nested type: DbCacheItem

        [Serializable]
        private sealed class DbCacheItem
        {
            public string Partition { get; set; }

            public string Key { get; set; }

            public byte[] SerializedValue { get; set; }

            public long UtcCreation { get; set; }

            public long? UtcExpiry { get; set; }

            public long? Interval { get; set; }
        }

        #endregion Nested type: DbCacheItem
    }
}