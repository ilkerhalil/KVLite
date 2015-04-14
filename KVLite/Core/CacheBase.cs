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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;
using CodeProject.ObjectPool;
using Common.Logging;
using Common.Logging.Simple;
using Dapper;
using Finsa.CodeServices.Clock;
using PommaLabs.KVLite.Utilities;
using PommaLabs.KVLite.Utilities.Extensions;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache.</typeparam>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class CacheBase<TCache, TCacheSettings> : FormattableObject, ICache<TCacheSettings>
        where TCache : CacheBase<TCache, TCacheSettings>, ICache<TCacheSettings>
        where TCacheSettings : CacheSettingsBase
    {
        #region Constants

        /// <summary>
        ///   The page size in bytes.
        /// </summary>
        private const int PageSizeInBytes = 32768;

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
        ///   "InsertionCountBeforeAutoClean" configuration parameter, then we must reset it and do
        ///   a SOFT cleanup.
        /// </summary>
        private short _insertionCount;

        /// <summary>
        ///   The cache settings.
        /// </summary>
        private readonly TCacheSettings _settings;

        /// <summary>
        ///   The clock instance, used to compute expiry times, etc etc.
        /// </summary>
        private readonly IClock _clock;

        /// <summary>
        ///   The log used by the cache.
        /// </summary>
        private readonly ILog _log;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="CacheBase{TCache, TCacheSettings}"/>
        ///   class with given settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        internal CacheBase(TCacheSettings settings, IClock clock, ILog log)
        {
            Contract.Requires<ArgumentNullException>(settings != null, ErrorMessages.NullSettings);
            _settings = settings;
            _clock = clock ?? new SystemClock();
            _log = log ?? new NoOpLogger();

            _settings.PropertyChanged += Settings_PropertyChanged;

            // Connection string must be customized by each cache.
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

            // Initial cleanup.
            Clear(CacheReadMode.ConsiderExpiryDate);
        }

        #endregion Construction

        #region Public Members

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
            const long pageSizeInKb = PageSizeInBytes / 1024L;
            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<long>("PRAGMA page_count;") * pageSizeInKb;
            }
        }

        #endregion Public Members

        #region ICache Members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>The clock used by the cache.</value>
        public IClock Clock
        {
            get { return _clock; }
        }

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        public ILog Log
        {
            get { return _log; }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        CacheSettingsBase ICache.Settings
        {
            get { return _settings; }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public TCacheSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        ///   Gets the value with the specified partition and key.
        /// </summary>
        /// <value>The value with the specified partition and key.</value>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified partition and key.</returns>
        public object this[string partition, string key]
        {
            get { return Get(partition, key); }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            DoAdd(partition, key, value, _clock.UtcNow + interval, interval);
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            DoAdd(partition, key, value, utcExpiry, null);
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        public void Clear()
        {
            DoClear(null);
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition"></param>
        public void Clear(string partition)
        {
            DoClear(partition);
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        public long LongCount()
        {
            return DoCount(null);
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        public long LongCount(string partition)
        {
            return DoCount(partition);
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will
        ///   be increased by corresponding interval.
        /// </summary>
        /// <returns>All cache items.</returns>
        public IList<CacheItem> GetManyItems()
        {
            return DoGetManyItems(null);
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        public IList<CacheItem> GetManyItems(string partition)
        {
            return DoGetManyItems(partition);
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <returns>All values, without updating expiry dates.</returns>
        public IList<CacheItem> PeekManyItems()
        {
            return DoPeekManyItems(null);
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        public IList<CacheItem> PeekManyItems(string partition)
        {
            return DoPeekManyItems(partition);
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        public bool Contains(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<int>(SQLiteQueries.Contains, p).First() > 0;
            }
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        public object Get(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<byte[]>(SQLiteQueries.GetOne, p).Select(DeserializeValue).FirstOrDefault(NotNull);
            }
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        public CacheItem GetItem(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetOneItem, p).Select(ToCacheItem).FirstOrDefault(NotNull);
            }
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public object Peek(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<byte[]>(SQLiteQueries.PeekOne, p).Select(DeserializeValue).FirstOrDefault(NotNull);
            }
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public CacheItem PeekItem(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.PeekOneItem, p).Select(ToCacheItem).FirstOrDefault(NotNull);
            }
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
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
        ///   Runs VACUUM on the underlying SQLite database.
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
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        /// <returns>The operation task.</returns>
        public Task VacuumAsync()
        {
            return TaskRunner.Run(Vacuum);
        }

        #endregion Public Methods

        #region Abstract Methods

        /// <summary>
        ///   Returns whether the changed property is the data source.
        /// </summary>
        /// <param name="changedPropertyName">Name of the changed property.</param>
        /// <returns>Whether the changed property is the data source.</returns>
        protected abstract bool DataSourceHasChanged(string changedPropertyName);

        /// <summary>
        ///   Gets the data source, that is, the location of the SQLite store (it may be a file path
        ///   or a memory URI).
        /// </summary>
        /// <param name="journalMode">The journal mode.</param>
        /// <returns>The SQLite data source that will be used by the cache.</returns>
        protected abstract string GetDataSource(out SQLiteJournalModeEnum journalMode);

        #endregion Abstract Methods

        #region Protected Methods

        /// <summary>
        ///   Initializes the SQLite native libraries.
        /// </summary>
        protected static void InitSQLite()
        {
            // Makes SQLite work... (loading dll from e.g. KVLite/x64/SQLite.Interop.dll)
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", nativePath);

            // Logs the path where SQLite has been set.
            Trace.WriteLine("SQLite native libraries set at {0}", nativePath);
        }

        #endregion Protected Methods

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

            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("serializedValue", serializedValue, DbType.Binary, size: serializedValue.Length);
            p.Add("utcExpiry", utcExpiry.HasValue ? DateTimeExtensions.ToUnixTime(utcExpiry.Value) : new long?(), DbType.Int64);
            p.Add("interval", interval.HasValue ? (long) interval.Value.TotalSeconds : new long?(), DbType.Int64);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Add, p);
            }

            // Insertion has concluded successfully, therefore we increment the operation counter.
            // If it has reached the "InsertionCountBeforeAutoClean" configuration parameter, then
            // we must reset it and do a SOFT cleanup. Following code is not fully thread safe, but
            // it does not matter, because the "InsertionCountBeforeAutoClean" parameter should be
            // just an hint on when to do the cleanup.
            if (++_insertionCount >= Settings.InsertionCountBeforeAutoClean)
            {
                _insertionCount = 0;
                TaskRunner.Run(() => Clear(CacheReadMode.ConsiderExpiryDate));
            }
        }

        private void DoClear(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

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
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

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
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetManyItems, p).Select(ToCacheItem).Where(NotNull).ToList();
            }
        }

        private IList<CacheItem> DoPeekManyItems(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

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
                PrepareRetries = 3,
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
                UtcCreation = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcCreation),
                UtcExpiry = src.UtcExpiry == null ? new DateTime?() : DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcExpiry.Value),
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

            /// <summary>
            ///   Returns all property (or field) values, along with their names, so that they can
            ///   be used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
            /// </summary>
            /// <returns>
            ///   Returns all property (or field) values, along with their names, so that they can
            ///   be used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
            /// </returns>
            protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
            {
                yield return GKeyValuePair.Create("Partition", Partition.SafeToString());
                yield return GKeyValuePair.Create("Key", Key.SafeToString());
                yield return GKeyValuePair.Create("UtcExpiry", UtcExpiry.SafeToString());
            }

            /// <summary>
            ///   Gets the identifying members.
            /// </summary>
            /// <returns>The identifying members.</returns>
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
