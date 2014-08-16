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
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Dapper;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class PersistentCache : CacheBase<PersistentCache>
    {
        private readonly string _connectionString;

        // This value is increased for each ADD operation; after this value reaches the "OperationCountBeforeSoftClear"
        // configuration parameter, then we must reset it and do a SOFT cleanup.
        private short _operationCount;

        #region Construction

        /// <summary>
        ///   TODO
        /// </summary>
        public PersistentCache() : this(Configuration.Instance.DefaultCachePath) {}

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="cachePath"></param>
        public PersistentCache(string cachePath)
        {
            var mappedCachePath = MapPath(cachePath);
            _connectionString = String.Format(Settings.Default.ConnectionStringFormat, mappedCachePath, Configuration.Instance.MaxCacheSizeInMB * 1024);

            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    if (!ctx.Exists(Queries.Ctor_SchemaIsReady)) {
                        // Creates the CacheItem table and the required indexes.
                        ctx.Connection.Execute(Queries.Ctor_CacheSchema);
                    }
                    // Sets DB settings
                    var pragmas = String.Format(Queries.Ctor_SetPragmas, Configuration.Instance.MaxLogSizeInMB*1024*1024);
                    ctx.Connection.Execute(pragmas);
                    scope.Complete();
                }
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
            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    ctx.Connection.Execute(Queries.Vacuum);
                    scope.Complete();
                }
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

        #region ICache<PersistentCache> Members

        public override void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, DateTime.UtcNow + interval, interval);
        }

        public override void AddStatic(string partition, string key, object value)
        {
            DoAdd(partition, key, value, null, null);
        }

        public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        public override void Clear(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    ctx.Connection.Execute(Queries.Clear, new {ignoreExpirationDate, DateTime.UtcNow});
                    scope.Complete();
                }
            }           
        }

        public override bool Contains(string partition, string key)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    var args = new {partition, key, DateTime.UtcNow};
                    var sliding = ctx.Connection.ExecuteScalar<bool?>(Queries.Contains_ItemExists, args);
                    if (!sliding.HasValue) {
                        return false;
                    }
                    if (sliding.Value) {
                        ctx.Connection.Execute(Queries.GetItem_UpdateExpiry, args);
                    }
                    scope.Complete();
                    return true;
                }
            }
        }

        public override long LongCount(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            // No need for a transaction, since it is just a select.
            using (var ctx = CacheContext.Create(_connectionString)) {
                return ctx.Connection.ExecuteScalar<long>(Queries.Count, new {ignoreExpirationDate, DateTime.UtcNow});
            }
        }

        public override object Get(string partition, string key)
        {
            var item = GetItem(partition, key);
            return item == null ? null : item.Value;
        }

        public override CacheItem GetItem(string partition, string key)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    var dbItem = ctx.Connection.Query<DbCacheItem>(Queries.GetItem_SelectItem, new {partition, key, DateTime.UtcNow}).FirstOrDefault();
                    if (dbItem == null) {
                        return null;
                    }
                    if (dbItem.Interval.HasValue) {
                        ctx.Connection.Execute(Queries.GetItem_UpdateExpiry, dbItem);
                    }
                    scope.Complete();
                    return new CacheItem(dbItem, BinarySerializer);
                }
            }
        }

        public override void Remove(string partition, string key)
        {
            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    ctx.Connection.Execute(Queries.Remove, new {partition, key});
                    scope.Complete();
                }
            }  
        }

        #endregion

        #region Private Methods

        private void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            // Serializing may be pretty expensive, therefore we keep it out of the transaction.
            var serializedValue = BinarySerializer.SerializeObject(value);

            using (var scope = CacheContext.OpenTransactionScope()) {
                using (var ctx = CacheContext.Create(_connectionString)) {
                    var dbItem = new DbCacheItem {
                        Partition = partition,
                        Key = key,
                        SerializedValue = serializedValue,
                        UtcCreation = DateTime.UtcNow.Ticks,
                        UtcExpiry = utcExpiry.HasValue ? utcExpiry.Value.Ticks : new long?(),
                        Interval = interval.HasValue ? interval.Value.Ticks : new long?()
                    };
                    ctx.Connection.Execute(Queries.DoAdd_InsertItem, dbItem);
                    scope.Complete();
                }
            }          

            // Operation has concluded successfully, therefore we increment the operation counter.
            // If it has reached the "OperationCountBeforeSoftClear" configuration parameter,
            // then we must reset it and do a SOFT cleanup.
            // Following code is not fully thread safe, but it does not matter, because the
            // "OperationCountBeforeSoftClear" parameter should be just an hint on when to do the cleanup.
            _operationCount++;
            if (_operationCount == Configuration.Instance.OperationCountBeforeSoftCleanup) {
                _operationCount = 0;
                Clear(CacheReadMode.ConsiderExpirationDate);
            }
        }

        private static string MapPath(string path)
        {
            Raise<ArgumentException>.IfIsEmpty(path, ErrorMessages.NullOrEmptyCachePath);
            return HttpContext.Current == null ? path : HttpContext.Current.Server.MapPath(path);
        }

        #endregion
    }
}