//
// PersistentCache.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using CodeProject.ObjectPool;
using Dapper;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class PersistentCache : CacheBase<PersistentCache>
    {
        private const string ConnectionStringFormat = @"Data Source={0};Version=3;Synchronous=Off;Journal Mode=WAL;DateTimeFormat=Ticks;Page Size=32768;Max Page Count={1};";

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly ParameterizedObjectPool<string, PooledObjectWrapper<SQLiteConnection>> ConnectionPool = new ParameterizedObjectPool<string, PooledObjectWrapper<SQLiteConnection>>(
            1, Configuration.Instance.MaxCachedConnectionCount, CreatePooledConnection);

        private readonly string _connectionString;

        // This value is increased for each ADD operation; after this value reaches the "OperationCountBeforeSoftClear"
        // configuration parameter, then we must reset it and do a SOFT cleanup.
        private short _operationCount;

        #region Construction

        /// <summary>
        ///   TODO
        /// </summary>
        public PersistentCache() : this(Configuration.Instance.DefaultCachePath)
        {
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="cachePath"></param>
        public PersistentCache(string cachePath)
        {
            Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(cachePath), ErrorMessages.NullOrEmptyCachePath);

            var mappedCachePath = HttpContext.Current == null ? cachePath : HttpContext.Current.Server.MapPath(cachePath);
            var maxPageCount = Configuration.Instance.MaxCacheSizeInMB*32; // Each page is 32KB large - Multiply by 1024*1024/32768
            _connectionString = String.Format(ConnectionStringFormat, mappedCachePath, maxPageCount);

            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                if (ctx.InternalResource.ExecuteScalar<long>(Queries.SchemaIsReady, trx) == 0)
                {
                    // Creates the CacheItem table and the required indexes.
                    ctx.InternalResource.Execute(Queries.CacheSchema, null, trx);
                }
                trx.Commit();
            }

            // Initial cleanup
            Clear(CacheReadMode.ConsiderExpirationDate);
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///   TODO
        /// </summary>
        public void Vacuum()
        {
            // Vacuum cannot be run within a transaction.
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            {
                ctx.InternalResource.Execute(Queries.Vacuum);
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

        #endregion

        #region ICache Members

        public override void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, DateTime.UtcNow + interval, interval);
        }

        public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        public override void Clear(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                ctx.InternalResource.Execute(Queries.Clear, new {ignoreExpirationDate}, trx);
                trx.Commit();
            }
        }

        public override bool Contains(string partition, string key)
        {
            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                var args = new {partition, key};
                var sliding = ctx.InternalResource.ExecuteScalar<bool?>(Queries.Contains, args, trx);
                if (!sliding.HasValue)
                {
                    return false;
                }
                if (sliding.Value)
                {
                    ctx.InternalResource.Execute(Queries.UpdateExpiry, args, trx);
                }
                trx.Commit();
            }
            return true;
        }

        public override long LongCount(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            // No need for a transaction, since it is just a select.
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            {
                return ctx.InternalResource.ExecuteScalar<long>(Queries.Count, new {ignoreExpirationDate});
            }
        }

        public override CacheItem GetItem(string partition, string key)
        {
            DbCacheItem dbItem;
            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                dbItem = ctx.InternalResource.Query<DbCacheItem>(Queries.GetItem, new {partition, key}, trx).FirstOrDefault();
                if (dbItem == null)
                {
                    return null;
                }
                if (dbItem.Interval.HasValue)
                {
                    ctx.InternalResource.Execute(Queries.UpdateExpiry, dbItem, trx);
                }
                trx.Commit();
            }
            return ToCacheItem(dbItem);
        }

        public override void Remove(string partition, string key)
        {
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                ctx.InternalResource.Execute(Queries.Remove, new {partition, key}, trx);
                trx.Commit();
            }
        }

        protected override IList<CacheItem> DoGetAllItems()
        {
            var items = new List<CacheItem>();
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                items.AddRange(ctx.InternalResource.Query<DbCacheItem>(Queries.DoGetAllItems, null, trx).Select(ToCacheItem));
                trx.Commit();
            }
            return items;
        }

        protected override IList<CacheItem> DoGetPartitionItems(string partition)
        {
            var items = new List<CacheItem>();
            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                items.AddRange(ctx.InternalResource.Query<DbCacheItem>(Queries.DoGetPartitionItems, new {partition}, trx).Select(ToCacheItem));
                trx.Commit();
            }
            return items;
        }

        #endregion

        #region Private Methods

        private static SQLiteTransaction BeginTransaction(PooledObjectWrapper<SQLiteConnection> connectionWrapper)
        {
            return connectionWrapper.InternalResource.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        private static PooledObjectWrapper<SQLiteConnection> CreatePooledConnection(string connectionString)
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            // Settings ten minutes as timeout should be more than enough...
            connection.DefaultTimeout = 600;
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Configuration.Instance.MaxLogSizeInMB*1024*1024;
            var pragmas = String.Format(Queries.SetPragmas, journalSizeLimitInBytes);
            connection.Execute(pragmas);
            return new PooledObjectWrapper<SQLiteConnection>(connection);
        }

        private void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            // Serializing may be pretty expensive, therefore we keep it out of the transaction.
            var serializedValue = Task.Factory.StartNew<byte[]>(BinarySerializer.SerializeObject, value);

            using (var ctx = ConnectionPool.GetObject(_connectionString))
            using (var trx = BeginTransaction(ctx))
            {
                var dbItem = new DbCacheItem
                {
                    Partition = partition,
                    Key = key,
                    SerializedValue = serializedValue.Result,
                    UtcExpiry = utcExpiry.HasValue ? (long) (utcExpiry.Value - UnixEpoch).TotalSeconds : new long?(),
                    Interval = interval.HasValue ? (long) interval.Value.TotalSeconds : new long?()
                };
                ctx.InternalResource.Execute(Queries.DoAdd, dbItem, trx);
                trx.Commit();
            }

            // Operation has concluded successfully, therefore we increment the operation counter.
            // If it has reached the "OperationCountBeforeSoftClear" configuration parameter,
            // then we must reset it and do a SOFT cleanup.
            // Following code is not fully thread safe, but it does not matter, because the
            // "OperationCountBeforeSoftClear" parameter should be just an hint on when to do the cleanup.
            _operationCount++;
            if (_operationCount == Configuration.Instance.OperationCountBeforeSoftCleanup)
            {
                _operationCount = 0;
                Clear(CacheReadMode.ConsiderExpirationDate);
            }
        }

        private static CacheItem ToCacheItem(DbCacheItem original)
        {
            return new CacheItem
            {
                Partition = original.Partition,
                Key = original.Key,
                Value = BinarySerializer.DeserializeObject(original.SerializedValue),
                UtcCreation = UnixEpoch.AddSeconds(original.UtcCreation),
                UtcExpiry = original.UtcExpiry == null ? new DateTime?() : UnixEpoch.AddSeconds(original.UtcExpiry.Value),
                Interval = original.Interval == null ? new TimeSpan?() : TimeSpan.FromSeconds(original.Interval.Value)
            };
        }

        #endregion

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
    }
}