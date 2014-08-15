using System;
using System.Data;
using System.Data.SQLite;
using System.IO;
using System.Web;
using KVLite.Core;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    public sealed class PersistentCache : ICache<PersistentCache>
    {
        private static readonly PersistentCache CachedDefaultInstance = new PersistentCache();

        private readonly BinarySerializer _binarySerializer = new BinarySerializer();
        private readonly string _connectionString;

        // This value is increased for each ADD operation; after this value reaches the "OperationCountBeforeSoftClear"
        // configuration parameter, then we must reset it and do a SOFT cleanup.
        private short _operationCount = 0;

        public PersistentCache() : this(Configuration.Instance.DefaultCachePath)
        {
        }

        public PersistentCache(string cachePath)
        {
            var mappedCachePath = MapPath(cachePath);
            _connectionString = String.Format(Settings.Default.ConnectionStringFormat, mappedCachePath, Configuration.Instance.MaxCacheSizeInMB);

            if (!File.Exists(mappedCachePath)) {
                // ???
            }

            using (var ctx = CacheContext.Create(_connectionString))
            using (var trx = ctx.Connection.BeginTransaction()) {
                try {
                    if (!ctx.Exists(Queries.SchemaIsReady, trx)) {
                        ctx.ExecuteNonQuery(Queries.CacheSchema, trx);
                        ctx.ExecuteNonQuery(Queries.IndexSchema, trx);
                    }

                    // Commit must be the _last_ instruction in the try block.
                    trx.Commit();
                } catch {
                    trx.Rollback();
                    throw;
                }
            }
        }

        public static PersistentCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        #region ICache Members

        public object AddPersistent(string partition, string key, object value)
        {
            return DoAdd(partition, key, value, null, null);
        }

        public object AddPersistent(string key, object value)
        {
            return AddPersistent(Settings.Default.DefaultPartition, key, value);
        }

        public void Clear()
        {
            Clear(CacheReadMode.ConsiderExpirationDate);
        }

        public void Clear(CacheReadMode cacheReadMode)
        {
            DoClear(cacheReadMode);
        }

        public int Count()
        {
            return (int) LongCount(CacheReadMode.ConsiderExpirationDate);
        }

        public int Count(CacheReadMode cacheReadMode)
        {
            return (int) LongCount(cacheReadMode);
        }

        public long LongCount()
        {
            return Count(CacheReadMode.ConsiderExpirationDate);
        }

        public long LongCount(CacheReadMode cacheReadMode)
        {
            var ignoreExpirationDate = (cacheReadMode == CacheReadMode.IgnoreExpirationDate);
            using (var ctx = CacheContext.Create(_connectionString))
            using (var cmd = new SQLiteCommand(Queries.Count, ctx.Connection))
            {
                cmd.Parameters.AddWithValue("ignoreExpirationDate", ignoreExpirationDate).DbType = DbType.Boolean;
                cmd.Parameters.AddWithValue("utcNow", DateTime.UtcNow).DbType = DbType.DateTime;
                return (long) cmd.ExecuteScalar();
            }
        }

        #endregion

        #region Private Methods

        private object DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            var serializedValue = _binarySerializer.SerializeObject(value);
            var expiry = utcExpiry.HasValue ? utcExpiry.Value as object : DBNull.Value;
            var ticks = interval.HasValue ? interval.Value.Ticks as object : DBNull.Value;

            using (var ctx = CacheContext.Create(_connectionString))
            using (var trx = ctx.Connection.BeginTransaction()) {
                try {
                    var item = DoGetOneItem(Queries.DoAdd_Select, ctx.Connection, trx, partition, key);

                    using (var cmd = ctx.Connection.CreateCommand())
                    {
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = item == null ? Queries.DoAdd_Insert : Queries.DoAdd_Update;
                        cmd.Transaction = trx;
                        cmd.Parameters.AddWithValue("partition", partition).DbType = DbType.String;
                        cmd.Parameters.AddWithValue("key", key).DbType = DbType.String;
                        cmd.Parameters.AddWithValue("serializedValue", serializedValue).DbType = DbType.Binary;
                        cmd.Parameters.AddWithValue("utcCreation", DateTime.UtcNow).DbType = DbType.DateTime;
                        cmd.Parameters.AddWithValue("utcExpiry", expiry).DbType = DbType.DateTime;
                        cmd.Parameters.AddWithValue("interval", ticks).DbType = DbType.Int64;
                        cmd.ExecuteNonQuery();

                        // Commit must be the _last_ instruction in the try block.
                        trx.Commit();
                    }
                } catch {
                    trx.Rollback();
                    throw;
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
                DoClear(CacheReadMode.ConsiderExpirationDate);
            }

            // Value is returned.
            return value;
        }

        private void DoClear(CacheReadMode readMode)
        {
            var ignoreExpirationDate = (readMode == CacheReadMode.IgnoreExpirationDate);
            using (var ctx = CacheContext.Create(_connectionString))
            using (var cmd = new SQLiteCommand(Queries.DoClear, ctx.Connection)) {
                cmd.Parameters.AddWithValue("ignoreExpirationDate", ignoreExpirationDate).DbType = DbType.Boolean;
                cmd.Parameters.AddWithValue("utcNow", DateTime.UtcNow).DbType = DbType.DateTime;
                cmd.ExecuteNonQuery();
            }
        }

        private CacheItem DoGet(string partition, string key)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            // For this kind of task, we need a transaction. In fact, since the value may be sliding,
            // we may have to issue an update following the initial select.
            using (var ctx = CacheContext.Create(_connectionString))
            using (var trx = ctx.Connection.BeginTransaction()) {
                try {
                    var item = DoGetOneItem(Queries.DoGet_Select, ctx.Connection, trx, partition, key);
                    if (item != null && item.Interval.HasValue) {
                        // Since item exists and it is sliding, then we need to update its expiration time.
                        item.UtcExpiry = item.UtcExpiry + TimeSpan.FromTicks(item.Interval.Value);
                        using (var cmd = new SQLiteCommand(Queries.DoGet_UpdateExpiry, ctx.Connection, trx)) {
                            cmd.Parameters.AddWithValue("partition", partition).DbType = DbType.String;
                            cmd.Parameters.AddWithValue("key", key).DbType = DbType.String;
                            cmd.Parameters.AddWithValue("utcExpiry", item.UtcExpiry).DbType = DbType.DateTime;
                        }
                    }
                    
                    // Commit must be the _last_ instruction in the try block, except for return.
                    trx.Commit();

                    // We return the item we just found (or null if it does not exist).
                    return item;
                } catch {
                    trx.Rollback();
                    throw;
                }             
            }  
        }

        private static CacheItem DoGetOneItem(string query, SQLiteConnection connection, SQLiteTransaction transaction, string partition, string key)
        {
            using (var cmd = new SQLiteCommand(query, connection, transaction))
            {
                cmd.Parameters.AddWithValue("Partition", partition).DbType = DbType.String;
                cmd.Parameters.AddWithValue("Key", key).DbType = DbType.String;
                cmd.Parameters.AddWithValue("UtcExpiry", DateTime.UtcNow).DbType = DbType.DateTime;
                var reader = cmd.ExecuteReader();
                if (!reader.NextResult())
                {
                    // Cache does not contain given item
                    return null;
                }
                return new CacheItem
                {
                    Partition = reader.GetString(CacheItem.PartitionDbIndex),
                    Key = reader.GetString(CacheItem.KeyDbIndex),
                    SerializedValue = reader.GetValue(2) as byte[],
                    UtcCreation = reader.GetDateTime(3),
                    UtcExpiry = reader.IsDBNull(3) ? new DateTime?() : reader.GetDateTime(3)
                };
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
