// File name: CacheBase.cs
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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using CodeProject.ObjectPool;
using Common.Logging;
using Dapper;
using PommaLabs.Extensions;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache.</typeparam>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class CacheBase<TCache, TCacheSettings> : FormattableObject, ICache<TCache, TCacheSettings>
        where TCache : CacheBase<TCache, TCacheSettings>, ICache<TCache, TCacheSettings>, new()
        where TCacheSettings : CacheSettingsBase, new()
    {
        #region Constants

        private const int PageSizeInBytes = 32768;

        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private static readonly TCache CachedDefaultInstance = new TCache();

        #endregion Constants

        #region Fields

        /// <summary>
        ///   The connection pool used to cache open connections.
        /// </summary>
        private ObjectPool<PooledObjectWrapper<SQLiteConnection>> _connectionPool;

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

        /// <summary>
        ///   This value is increased for each ADD operation; after this value reaches the
        ///   "InsertionCountBeforeAutoClean" configuration parameter, then we must reset it and do a
        ///   SOFT cleanup.
        /// </summary>
        private short _insertionCount;

        private readonly TCacheSettings _settings;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   </summary>
        /// <param name="settings"></param>
        internal CacheBase(TCacheSettings settings)
        {
            Contract.Requires<ArgumentNullException>(settings != null);
            _settings = settings;

            settings.PropertyChanged += Settings_PropertyChanged;

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

        #region Public Members

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        [Pure]
        public static TCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        /// <summary>
        ///   Gets the value with the specified key and belonging to the default partition.
        /// </summary>
        /// <value>The value with the specified key and belonging to the default partition.</value>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified key and belonging to the default partition.</returns>
        [Pure]
        public object this[string key]
        {
            get { return Get(Settings.DefaultPartition, key); }
        }

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        [Pure]
        public long CacheSizeInKB()
        {
            const long pageSizeInKb = PageSizeInBytes/1024L;
            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<long>("PRAGMA page_count;") * pageSizeInKb;
            }
        }

        #endregion Public Properties

        #region ICache Members

        CacheSettingsBase ICache.Settings
        {
            get { return _settings; }
        }

        public TCacheSettings Settings
        {
            get { return _settings; }
        }

        public object this[string partition, string key]
        {
            get { return Get(partition, key); }
        }

        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, DateTime.UtcNow + interval, interval);
        }

        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        public void Clear()
        {
            DoClear(null);
        }

        public void Clear(string partition)
        {
            DoClear(partition);
        }

        public long LongCount()
        {
            return DoCount(null);
        }

        public long LongCount(string partition)
        {
            return DoCount(partition);
        }

        public IList<CacheItem> GetManyItems()
        {
            return DoGetManyItems(null);
        }

        public IList<CacheItem> GetManyItems(string partition)
        {
            return DoGetManyItems(partition);
        }

        public IList<CacheItem> PeekManyItems()
        {
            return DoPeekManyItems(null);
        }

        public IList<CacheItem> PeekManyItems(string partition)
        {
            return DoPeekManyItems(partition);
        }

        public CacheKind Kind
        {
            get { return CacheKind.Persistent; }
        }

        public bool Contains(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<int>(SQLiteQueries.Contains, p).First() > 0;
            }
        }

        public object Get(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<byte[]>(SQLiteQueries.GetOne, p).Select(DeserializeValue).FirstOrDefault(NotNull);
            }
        }

        public CacheItem GetItem(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetOneItem, p).Select(ToCacheItem).FirstOrDefault(NotNull);
            }
        }

        public object Peek(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<byte[]>(SQLiteQueries.PeekOne, p).Select(DeserializeValue).FirstOrDefault(NotNull);
            }
        }

        public CacheItem PeekItem(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.PeekOneItem, p).Select(ToCacheItem).FirstOrDefault(NotNull);
            }
        }

        public void Remove(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Remove, p);
            }
        }

        #endregion ICache Members

        #region Public Methods

        public void Clear(CacheReadMode cacheReadMode)
        {
            DoClear(null, cacheReadMode);
        }

        public void Clear(string partition, CacheReadMode cacheReadMode)
        {
            DoClear(partition, cacheReadMode);
        }

        public int Count(CacheReadMode cacheReadMode)
        {
            return Convert.ToInt32(DoCount(null, cacheReadMode));
        }

        public int Count(string partition, CacheReadMode cacheReadMode)
        {
            return Convert.ToInt32(DoCount(partition, cacheReadMode));
        }

        public long LongCount(CacheReadMode cacheReadMode)
        {
            return DoCount(null, cacheReadMode);
        }

        public long LongCount(string partition, CacheReadMode cacheReadMode)
        {
            return DoCount(partition, cacheReadMode);
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
            return TaskRunner.Run(Vacuum);
        }

        #endregion Public Methods

        #region Abstract Methods

        protected abstract bool DataSourceHasChanged(string changedPropertyName);

        protected abstract string GetDataSource(out SQLiteJournalModeEnum journalMode);

        #endregion Abstract Methods

        #region Protected Methods

        protected static void InitSQLite()
        {
            // Makes SQLite work... (loading dll from e.g. KVLite/x64/SQLite.Interop.dll)
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", nativePath);

            // Logs the path where SQLite has been set.
            LogManager.GetLogger<PersistentCache>().InfoFormat("SQLite native libraries set at {0}", nativePath);
        }

        #endregion

        #region Private Methods

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
                UtcExpiry = utcExpiry.HasValue ? (long) (utcExpiry.Value - UnixEpoch).TotalSeconds : new long?(),
                Interval = interval.HasValue ? (long) interval.Value.TotalSeconds : new long?()
            };

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Add, dbItem);
            }

            // Insertion has concluded successfully, therefore we increment the operation counter.
            // If it has reached the "InsertionCountBeforeAutoClean" configuration parameter, then we
            // must reset it and do a SOFT cleanup. Following code is not fully thread safe, but it
            // does not matter, because the "InsertionCountBeforeAutoClean" parameter should be just
            // an hint on when to do the cleanup.
            _insertionCount++;
            if (_insertionCount >= Settings.InsertionCountBeforeAutoClean)
            {
                _insertionCount = 0;
                Task.Factory.StartNew(() => Clear(CacheReadMode.ConsiderExpiryDate));
            }
        }

        private void DoClear(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Clear, p);
            }
        }

        private long DoCount(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);

            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<long>(SQLiteQueries.Count, p);
            }
        }

        private IList<CacheItem> DoGetManyItems(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetManyItems, p).Select(ToCacheItem).Where(NotNull).ToList();
            }
        }

        private IList<CacheItem> DoPeekManyItems(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.PeekManyItems, p).Select(ToCacheItem).Where(NotNull).ToList();
            }
        }

        private PooledObjectWrapper<SQLiteConnection> CreatePooledConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
            var walAutoCheckpointInPages = journalSizeLimitInBytes / PageSizeInBytes / 3;
            var pragmas = String.Format(SQLiteQueries.SetPragmas, PageSizeInBytes, journalSizeLimitInBytes, walAutoCheckpointInPages);
            connection.Execute(pragmas);
            return new PooledObjectWrapper<SQLiteConnection>(connection);
        }

        private static object DeserializeValue(byte[] serializedValue)
        {
            try
            {
                return BinarySerializer.DeserializeObject(serializedValue);
            }
            catch
            {
                // Something wrong happened during deserialization. Therefore, we act as if there
                // was no value.
                return null;
            }
        }

        private void InitConnectionString()
        {
            SQLiteJournalModeEnum journalMode;
            var cacheUri = GetDataSource(out journalMode);

            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                BinaryGUID = true,
                BrowsableConnectionString = false,
                /* Number of pages of 32KB */
                CacheSize = 128,
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,
                /* Settings ten minutes as timeout should be more than enough... */
                DefaultTimeout = 600,
                Enlist = false,
                FailIfMissing = false,
                ForeignKeys = false,
                FullUri = cacheUri,
                JournalMode = journalMode,
                LegacyFormat = false,
                /* Each page is 32KB large - Multiply by 1024*1024/32768 */
                MaxPageCount = Settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes, 
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

        private static bool NotNull<T>(T obj)
        {
            return !ReferenceEquals(obj, null);
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataSourceHasChanged(e.PropertyName))
            {
                InitConnectionString();
            }
        }

        private static CacheItem ToCacheItem(DbCacheItem src)
        {
            object deserializedValue;
            try
            {
                deserializedValue = BinarySerializer.DeserializeObject(src.SerializedValue);
            }
            catch
            {
                // Something wrong happened during deserialization. Therefore, we act as if there
                // was no element.
                return null;
            }
            return new CacheItem
            {
                Partition = src.Partition,
                Key = src.Key,
                Value = deserializedValue,
                UtcCreation = UnixEpoch.AddSeconds(src.UtcCreation),
                UtcExpiry = src.UtcExpiry == null ? new DateTime?() : UnixEpoch.AddSeconds(src.UtcExpiry.Value),
                Interval = src.Interval == null ? new TimeSpan?() : TimeSpan.FromSeconds(src.Interval.Value)
            };
        }

        #endregion Private Methods

        #region Nested type: DbCacheItem

        [Serializable]
        private sealed class DbCacheItem : EquatableObject<DbCacheItem>
        {
            #region Public Properties

            public string Partition { get; set; }

            public string Key { get; set; }

            public byte[] SerializedValue { get; set; }

            public long UtcCreation { get; set; }

            public long? UtcExpiry { get; set; }

            public long? Interval { get; set; }

            #endregion Public Properties

            #region EquatableObject<CacheItem> Members

            protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
            {
                yield return GKeyValuePair.Create("Partition", Partition.SafeToString());
                yield return GKeyValuePair.Create("Key", Key.SafeToString());
                yield return GKeyValuePair.Create("UtcExpiry", UtcExpiry.SafeToString());
            }

            protected override IEnumerable<object> GetIdentifyingMembers()
            {
                yield return Partition;
                yield return Key;
            }

            #endregion EquatableObject<CacheItem> Members
        }

        #endregion Nested type: DbCacheItem
    }
}