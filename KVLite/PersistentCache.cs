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
using System.Data;
using System.Data.SQLite;
using System.Web;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
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
            _connectionString = String.Format(Settings.Default.ConnectionStringFormat, mappedCachePath, Configuration.Instance.MaxCacheSizeInMB);

            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var trx = ctx.Connection.BeginTransaction()) {
                    try {
                        if (!ctx.Exists(Queries.Ctor_SchemaIsReady, trx)) {
                            // Creates the CacheItem table and the required indexes.
                            ctx.ExecuteNonQuery(Queries.Ctor_CacheSchema, trx);
                        }
                        // Sets DB settings
                        ctx.ExecuteNonQuery(Queries.Ctor_SetPragmas, trx);
                        // Commit must be the _last_ instruction in the try block.
                        trx.Commit();
                    } catch {
                        trx.Rollback();
                        throw;
                    }
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
            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var cmd = new SQLiteCommand(Queries.Vacuum, ctx.Connection)) {
                    cmd.ExecuteNonQuery();
                }
            }
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
            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var cmd = new SQLiteCommand(Queries.Clear, ctx.Connection)) {
                    cmd.Parameters.AddWithValue("ignoreExpirationDate", ignoreExpirationDate);
                    cmd.Parameters.AddWithValue("utcNow", DateTime.UtcNow);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        public override bool Contains(string partition, string key)
        {
            return DoGet(partition, key) != null;
        }

        public override long LongCount(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var cmd = new SQLiteCommand(Queries.Count, ctx.Connection)) {
                    cmd.Parameters.AddWithValue("ignoreExpirationDate", ignoreExpirationDate);
                    cmd.Parameters.AddWithValue("utcNow", DateTime.UtcNow);
                    return (long) cmd.ExecuteScalar(CommandBehavior.SingleResult);
                }
            }
        }

        public override object Get(string partition, string key)
        {
            var item = DoGet(partition, key);
            return item == null ? null : BinarySerializer.DeserializeObject(item.SerializedValue);
        }

        public override CacheItem GetItem(string partition, string key)
        {
            var item = DoGet(partition, key);
            if (item == null) {
                return null;
            }
            item.Value = BinarySerializer.DeserializeObject(item.SerializedValue);
            return item;
        }

        #endregion

        #region Private Methods

        private void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            var serializedValue = BinarySerializer.SerializeObject(value);
            var expiry = utcExpiry.HasValue ? utcExpiry.Value as object : DBNull.Value;
            var ticks = interval.HasValue ? interval.Value.Ticks as object : DBNull.Value;

            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var cmd = new SQLiteCommand(Queries.DoAdd, ctx.Connection)) {
                    cmd.Parameters.AddWithValue("partition", partition);
                    cmd.Parameters.AddWithValue("key", key);
                    cmd.Parameters.AddWithValue("serializedValue", serializedValue);
                    cmd.Parameters.AddWithValue("utcCreation", DateTime.UtcNow);
                    cmd.Parameters.AddWithValue("utcExpiry", expiry);
                    cmd.Parameters.AddWithValue("interval", ticks);
                    cmd.ExecuteNonQuery();
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

        private CacheItem DoGet(string partition, string key)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var ctx = CacheContext.Create(_connectionString)) {
                using (var cmd = new SQLiteCommand(Queries.DoGet, ctx.Connection)) {
                    cmd.Parameters.AddWithValue("partition", partition);
                    cmd.Parameters.AddWithValue("key", key);
                    cmd.Parameters.AddWithValue("utcNow", DateTime.UtcNow);
                    var reader = cmd.ExecuteReader(CommandBehavior.SingleRow);
                    if (!reader.Read()) {
                        // Cache does not contain given item
                        return null;
                    }
                    return new CacheItem {
                        Partition = reader.GetString(CacheItem.PartitionDbIndex),
                        Key = reader.GetString(CacheItem.KeyDbIndex),
                        SerializedValue = reader.GetValue(CacheItem.SerializedValueDbIndex) as byte[],
                        UtcCreation = reader.GetDateTime(CacheItem.UtcCreationDbIndex),
                        UtcExpiry = reader.IsDBNull(CacheItem.UtcExpiryDbIndex) ? new DateTime?() : reader.GetDateTime(CacheItem.UtcExpiryDbIndex),
                        Interval = reader.IsDBNull(CacheItem.IntervalDbIndex) ? new TimeSpan?() : TimeSpan.FromTicks(reader.GetInt64(CacheItem.IntervalDbIndex))
                    };
                }
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