// File name: AbstractSQLiteCache.cs
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
using System.Runtime.Serialization.Formatters;
using System.Threading;
using CodeProject.ObjectPool;
using Common.Logging;
using Dapper;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using PommaLabs.Thrower;
using Finsa.CodeServices.Common.Extensions;
using Finsa.CodeServices.Common.IO.RecyclableMemoryStream;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using Task = System.Threading.Tasks.Task;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class AbstractSQLiteCache<TCacheSettings> : AbstractCache<TCacheSettings>
        where TCacheSettings : AbstractCacheSettings
    {
        #region Constants

        /// <summary>
        ///   The initial value for the variable which keeps the auto clean counter.
        /// </summary>
        const int InsertionCountStart = 0;

        /// <summary>
        ///   The default SQLite page size in bytes. Do not change this value unless SQLite changes
        ///   its defaults. WAL journal does limit the capability to change that value even when the
        ///   DB is still empty.
        /// </summary>
        const int PageSizeInBytes = 1024;

        /// <summary>
        ///   The string used to tag streams coming from <see cref="RecyclableMemoryStreamManager.Instance"/>.
        /// </summary>
        const string StreamTag = nameof(KVLite);

        /// <summary>
        ///   The initial capacity of the streams retrieved from <see cref="RecyclableMemoryStreamManager.Instance"/>.
        /// </summary>
        const int InitialStreamCapacity = 1024;

        #endregion Constants

        #region Fields

        /// <summary>
        ///   The connection pool used to cache open connections.
        /// </summary>
        ObjectPool<PooledObjectWrapper<SQLiteConnection>> _connectionPool;

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        string _connectionString;

        /// <summary>
        ///   This value is increased for each ADD operation; after this value reaches the
        ///   "InsertionCountBeforeAutoClean" configuration parameter, then we must reset it and do
        ///   a SOFT cleanup.
        /// </summary>
        int _insertionCount = InsertionCountStart;

        /// <summary>
        ///   The cache settings.
        /// </summary>
        readonly TCacheSettings _settings;

        /// <summary>
        ///   The clock instance, used to compute expiry times, etc etc.
        /// </summary>
        readonly IClock _clock;

        /// <summary>
        ///   The log used by the cache.
        /// </summary>
        readonly ILog _log;

        /// <summary>
        ///   The serializer used by the cache.
        /// </summary>
        readonly ISerializer _serializer;

        /// <summary>
        ///   The compressor used by the cache.
        /// </summary>
        readonly ICompressor _compressor;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="AbstractSQLiteCache{TCacheSettings}"/>
        ///   class with given settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        internal AbstractSQLiteCache(TCacheSettings settings, IClock clock, ILog log, ISerializer serializer, ICompressor compressor)
        {
            // Preconditions
            Raise<ArgumentNullException>.IfIsNull(settings, ErrorMessages.NullSettings);
            
            _settings = settings;
            _clock = clock ?? new SystemClock();
            _log = log ?? LogManager.GetLogger(GetType());
            _compressor = compressor ?? new LZ4Compressor();
            _serializer = serializer ?? new JsonSerializer(new JsonSerializerSettings
            {
                DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.IgnoreAndPopulate,
                FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.String,
                Formatting = Newtonsoft.Json.Formatting.None,
                NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None,
                ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple,
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All
            });

            _settings.PropertyChanged += Settings_PropertyChanged;

            // Connection string must be customized by each cache.
            InitConnectionString();

            using (var ctx = _connectionPool.GetObject())
            using (var trx = ctx.InternalResource.BeginTransaction())
            {
                if (ctx.InternalResource.ExecuteScalar<long>(SQLiteQueries.IsSchemaReady, trx) == 0)
                {
                    // Creates the CacheItem table and the required indexes.
                    ctx.InternalResource.Execute(SQLiteQueries.CacheSchema, null, trx);
                }
                trx.Commit();
            }

            // Initial cleanup.
            Clear(CacheReadMode.ConsiderExpiryDate);
        }

        #endregion Construction

        #region Public Members

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        [Pure]
        public long CacheSizeInKB()
        {
            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                var pageCount = ctx.InternalResource.ExecuteScalar<long>("PRAGMA page_count;");
                var freelistCount = ctx.InternalResource.ExecuteScalar<long>("PRAGMA freelist_count;");
                var pageSizeInKB = ctx.InternalResource.ExecuteScalar<long>("PRAGMA page_size;") / 1024L;
                return (pageCount - freelistCount) * pageSizeInKB;
            }
        }

        /// <summary>
        ///   Clears the cache using the specified cache read mode.
        /// </summary>
        /// <param name="cacheReadMode">The cache read mode.</param>
        public void Clear(CacheReadMode cacheReadMode)
        {
            ClearInternal(null, cacheReadMode);
        }

        /// <summary>
        ///   Clears the specified partition using the specified cache read mode.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        public void Clear(string partition, CacheReadMode cacheReadMode)
        {
            ClearInternal(partition, cacheReadMode);
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public int Count(CacheReadMode cacheReadMode)
        {
            return Convert.ToInt32(CountInternal(null, cacheReadMode));
        }

        /// <summary>
        ///   The number of items in the cache for given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public int Count(string partition, CacheReadMode cacheReadMode)
        {
            return Convert.ToInt32(CountInternal(partition, cacheReadMode));
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public long LongCount(CacheReadMode cacheReadMode)
        {
            return CountInternal(null, cacheReadMode);
        }

        /// <summary>
        ///   The number of items in the cache for given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public long LongCount(string partition, CacheReadMode cacheReadMode)
        {
            return CountInternal(partition, cacheReadMode);
        }

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            _log.InfoFormat("Vacuuming the SQLite DB '{0}'...", Settings.CacheUri);

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

        #endregion Public Members

        #region ICache Members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>The clock used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public sealed override IClock Clock
        {
            get { return _clock; }
        }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="DeflateCompressor"/>.
        /// </remarks>
        public sealed override ICompressor Compressor
        {
            get { return _compressor; }
        }

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to what
        ///   <see cref="LogManager.GetLogger(System.Type)"/> returns.
        /// </remarks>
        public sealed override ILog Log
        {
            get { return _log; }
        }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>.
        ///   Therefore, if you do not specify another serializer, make sure that your objects are
        ///   serializable (in most cases, simply use the <see cref="SerializableAttribute"/> and expose fields as public properties).
        /// </remarks>
        public sealed override ISerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public sealed override TCacheSettings Settings
        {
            get { return _settings; }
        }

        /// <summary>
        ///   True if the Peek methods are implemented, false otherwise.
        /// </summary>
        public override bool CanPeek => true;

        #endregion ICache Members

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

        #region Private Methods

        protected sealed override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval)
        {
            // Serializing may be pretty expensive, therefore we keep it out of the transaction.
            byte[] serializedValue;
            try
            {
                using (var memoryStream = RecyclableMemoryStreamManager.Instance.GetStream(StreamTag, InitialStreamCapacity))
                {
                    using (var compressionStream = _compressor.CreateCompressionStream(memoryStream))
                    {
                        _serializer.SerializeToStream(value, compressionStream);
                    }
                    serializedValue = memoryStream.ToArray();
                }
            }
            catch (Exception ex)
            {
                _log.ErrorFormat("Could not serialize given value '{0}'", ex, value.SafeToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add(nameof(serializedValue), serializedValue, DbType.Binary, size: serializedValue.Length);
            p.Add(nameof(utcExpiry), utcExpiry.ToUnixTime(), DbType.Int64);
            p.Add(nameof(interval), (long) interval.TotalSeconds, DbType.Int64);
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
            _insertionCount++;
            var oldInsertionCount = Interlocked.CompareExchange(ref _insertionCount, InsertionCountStart, Settings.InsertionCountBeforeAutoClean);
            if (oldInsertionCount == Settings.InsertionCountBeforeAutoClean)
            {
                // If they were equal, then we need to run the maintenance cleanup.
                TaskRunner.Run(() => Clear(CacheReadMode.ConsiderExpiryDate));
            }
        }

        protected sealed override void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Clear, p);
            }
        }

        protected sealed override bool ContainsInternal(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<int>(SQLiteQueries.Contains, p) > 0;
            }
        }

        protected sealed override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate), DbType.Boolean);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<long>(SQLiteQueries.Count, p);
            }
        }

        protected sealed override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var serializedValue = ctx.InternalResource.ExecuteScalar<byte[]>(SQLiteQueries.GetOne, p);
                return DeserializeValue<TVal>(serializedValue, partition, key);
            }
        }

        protected sealed override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var tmp = ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetOneItem, p, buffered: false).FirstOrDefault();
                return DeserializeCacheItem<TVal>(tmp);
            }
        }

        protected sealed override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource
                    .Query<DbCacheItem>(SQLiteQueries.GetManyItems, p, buffered: false)
                    .Select(DeserializeCacheItem<TVal>)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToArray();
            }
        }

        protected sealed override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var serializedValue = ctx.InternalResource.ExecuteScalar<byte[]>(SQLiteQueries.PeekOne, p);
                return DeserializeValue<TVal>(serializedValue, partition, key);
            }
        }

        protected sealed override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var tmp = ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.PeekOneItem, p, buffered: false).FirstOrDefault();
                return DeserializeCacheItem<TVal>(tmp);
            }
        }

        protected sealed override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource
                    .Query<DbCacheItem>(SQLiteQueries.PeekManyItems, p, buffered: false)
                    .Select(DeserializeCacheItem<TVal>)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToArray();
            }
        }

        protected sealed override void RemoveInternal(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add(nameof(partition), partition, DbType.String);
            p.Add(nameof(key), key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Remove, p);
            }
        }

        TVal UnsafeDeserializeValue<TVal>(byte[] serializedValue)
        {
            using (var memoryStream = RecyclableMemoryStreamManager.Instance.GetStream(StreamTag, serializedValue, 0, serializedValue.Length))
            using (var decompressionStream = _compressor.CreateDecompressionStream(memoryStream))
            {
                return _serializer.DeserializeFromStream<TVal>(decompressionStream);
            }
        }

        Option<TVal> DeserializeValue<TVal>(byte[] serializedValue, string partition, string key)
        {
            if (serializedValue == null)
            {
                // Nothing to deserialize, return None.
                return Option.None<TVal>();
            }
            try
            {
                return Option.Some(UnsafeDeserializeValue<TVal>(serializedValue));
            }
            catch (Exception ex)
            {
                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(partition, key);
                _log.Warn("Something wrong happened during deserialization", ex);
                return Option.None<TVal>();
            }
        }

        Option<CacheItem<TVal>> DeserializeCacheItem<TVal>(DbCacheItem src)
        {
            if (src == null)
            {
                // Nothing to deserialize, return None.
                return Option.None<CacheItem<TVal>>();
            }
            try
            {
                return Option.Some(new CacheItem<TVal>
                {
                    Partition = src.Partition,
                    Key = src.Key,
                    Value = UnsafeDeserializeValue<TVal>(src.SerializedValue),
                    UtcCreation = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcCreation),
                    UtcExpiry = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcExpiry),
                    Interval = TimeSpan.FromSeconds(src.Interval)
                });
            }
            catch (Exception ex)
            {
                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(src.Partition, src.Key);
                _log.Warn("Something wrong happened during deserialization", ex);
                return Option.None<CacheItem<TVal>>();
            }
        }

        PooledObjectWrapper<SQLiteConnection> CreatePooledConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
            var walAutoCheckpointInPages = journalSizeLimitInBytes / PageSizeInBytes / 3;
            var pragmas = string.Format(SQLiteQueries.SetPragmas, journalSizeLimitInBytes, walAutoCheckpointInPages);
            connection.Execute(pragmas);
            return new PooledObjectWrapper<SQLiteConnection>(connection);
        }

        void InitConnectionString()
        {
            SQLiteJournalModeEnum journalMode;
            var cacheUri = GetDataSource(out journalMode);

            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                BinaryGUID = true,
                BrowsableConnectionString = false,
                /* Number of pages of 1KB */
                CacheSize = 8192,
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,
                /* Settings three minutes as timeout should be more than enough... */
                DefaultTimeout = 180,
                Enlist = false,
                FailIfMissing = false,
                ForeignKeys = false,
                FullUri = cacheUri,
                JournalMode = journalMode,
                LegacyFormat = false,
                /* Each page is 1KB large - Multiply by 1024*1024/32768 */
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

        void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataSourceHasChanged(e.PropertyName))
            {
                InitConnectionString();
            }
        }

        #endregion Private Methods

        #region Nested type: DbCacheItem

        /// <summary>
        ///   Represents a row in the cache table.
        /// </summary>
        [Serializable]
        sealed class DbCacheItem : EquatableObject<DbCacheItem>
        {
            #region Public Properties

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Partition { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Key { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public byte[] SerializedValue { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public long UtcCreation { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public long UtcExpiry { get; set; }

            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public long Interval { get; set; }

            #endregion Public Properties

            #region EquatableObject<DbCacheItem> Members

            /// <summary>
            ///   Returns all property (or field) values, along with their names, so that they can
            ///   be used to produce a meaningful <see cref="M:Finsa.CodeServices.Common.FormattableObject.ToString"/>.
            /// </summary>
            protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
            {
                yield return KeyValuePair.Create(nameof(Partition), Partition.SafeToString());
                yield return KeyValuePair.Create(nameof(Key), Key.SafeToString());
                yield return KeyValuePair.Create(nameof(UtcExpiry), UtcExpiry.SafeToString());
            }

            /// <summary>
            ///   Gets the identifying members.
            /// </summary>
            protected override IEnumerable<object> GetIdentifyingMembers()
            {
                yield return Partition;
                yield return Key;
            }

            #endregion EquatableObject<DbCacheItem> Members
        }

        #endregion Nested type: DbCacheItem
    }
}