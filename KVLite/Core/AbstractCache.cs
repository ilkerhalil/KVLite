// File name: AbstractCache.cs
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
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using CodeProject.ObjectPool;
using Common.Logging;
using Common.Logging.Simple;
using Dapper;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using FSharpx;
using Microsoft.FSharp.Core;
using PommaLabs.KVLite.Utilities;
using PommaLabs.KVLite.Utilities.Extensions;
using Task = System.Threading.Tasks.Task;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class AbstractCache<TCacheSettings> : FormattableObject, ICache<TCacheSettings>
        where TCacheSettings : AbstractCacheSettings
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

        /// <summary>
        ///   The serializer used by the cache.
        /// </summary>
        private readonly ISerializer _serializer;

        /// <summary>
        ///   The compressor used by the cache.
        /// </summary>
        private readonly ICompressor _compressor;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="AbstractCache{TCacheSettings}"/>
        ///   class with given settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        internal AbstractCache(TCacheSettings settings, IClock clock, ILog log, ISerializer serializer, ICompressor compressor)
        {
            Contract.Requires<ArgumentNullException>(settings != null, ErrorMessages.NullSettings);
            _settings = settings;
            _clock = clock ?? new SystemClock();
            _log = log ?? new NoOpLogger();
            _compressor = compressor ?? new DeflateCompressor();
            _serializer = serializer ?? new BinarySerializer(new BinarySerializerSettings
            {
                // In simple mode, the assembly used during deserialization need not match exactly
                // the assembly used during serialization. Specifically, the version numbers need
                // not match as the LoadWithPartialName method is used to load the assembly.
                AssemblyFormat = FormatterAssemblyStyle.Simple,

                // The low deserialization level for .NET Framework remoting. It supports types
                // associated with basic remoting functionality.
                FilterLevel = TypeFilterLevel.Low,

                // Indicates that types can be stated only for arrays of objects, object members of
                // type Object, and ISerializable non-primitive value types. The XsdString and
                // TypesWhenNeeded settings are meant for high performance serialization between
                // services built on the same version of the .NET Framework. These two values do not
                // support VTS (Version Tolerant Serialization) because they intentionally omit type
                // information that VTS uses to skip or add optional fields and properties. You
                // should not use the XsdString or TypesWhenNeeded type formats when serializing and
                // deserializing types on a computer running a different version of the .NET
                // Framework than the computer on which the type was serialized. Serializing and
                // deserializing on computers running different versions of the .NET Framework
                // causes the formatter to skip serialization of type information, thus making it
                // impossible for the deserializer to skip optional fields if they are not present
                // in certain types that may exist in the other version of the .NET Framework. If
                // you must use XsdString or TypesWhenNeeded in such a scenario, you must provide
                // custom serialization for types that have changed from one version of the .NET
                // Framework to the other.
                TypeFormat = FormatterTypeStyle.TypesWhenNeeded,
            });

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
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        public ICompressor Compressor
        {
            get { return _compressor; }
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
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        public ISerializer Serializer
        {
            get { return _serializer; }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        AbstractCacheSettings ICache.Settings
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
        public FSharpOption<object> this[string partition, string key]
        {
            get { return Get<object>(partition, key); }
        }

        /// <summary>
        ///   Gets the value with the specified key and belonging to the default partition.
        /// </summary>
        /// <value>The value with the specified key and belonging to the default partition.</value>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified key and belonging to the default partition.</returns>
        [Pure]
        public FSharpOption<object> this[string key]
        {
            get { return Get<object>(Settings.DefaultPartition, key); }
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
        public void AddSliding<TVal>(string partition, string key, TVal value, TimeSpan interval)
        {
            AddInternal(partition, key, value, _clock.UtcNow + interval, interval);
        }

        public void AddSliding<TVal>(string key, TVal value, TimeSpan interval)
        {
            AddInternal(Settings.DefaultPartition, key, value, _clock.UtcNow + interval, interval);
        }

        public void AddStatic<TVal>(string partition, string key, TVal value)
        {
            AddInternal(partition, key, value, _clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval);
        }

        public void AddStatic<TVal>(string key, TVal value)
        {
            AddInternal(Settings.DefaultPartition, key, value, _clock.UtcNow + Settings.StaticInterval, Settings.StaticInterval);
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        public void AddTimed<TVal>(string partition, string key, TVal value, DateTime utcExpiry)
        {
            AddInternal(partition, key, value, utcExpiry, null);
        }

        public void AddTimed<TVal>(string key, TVal value, DateTime utcExpiry)
        {
            AddInternal(Settings.DefaultPartition, key, value, utcExpiry, null);
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        public void Clear()
        {
            ClearInternal(null);
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition"></param>
        public void Clear(string partition)
        {
            ClearInternal(partition);
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        public bool Contains(string partition, string key)
        {
            return ContainsInternal(partition, key);
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        public bool Contains(string key)
        {
            return ContainsInternal(Settings.DefaultPartition, key);
        }

        public int Count()
        {
            return Convert.ToInt32(CountInternal(null));
        }

        public int Count(string partition)
        {
            return Convert.ToInt32(CountInternal(partition));
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        public long LongCount()
        {
            return CountInternal(null);
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        public long LongCount(string partition)
        {
            return CountInternal(partition);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        public FSharpOption<TVal> Get<TVal>(string partition, string key)
        {
            return GetInternal<TVal>(partition, key);
        }

        public FSharpOption<TVal> Get<TVal>(string key)
        {
            return GetInternal<TVal>(Settings.DefaultPartition, key);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        public FSharpOption<CacheItem<TVal>> GetItem<TVal>(string partition, string key)
        {
            return GetItemInternal<TVal>(partition, key);
        }

        public FSharpOption<CacheItem<TVal>> GetItem<TVal>(string key)
        {
            return GetItemInternal<TVal>(Settings.DefaultPartition, key);
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will
        ///   be increased by corresponding interval.
        /// </summary>
        /// <returns>All cache items.</returns>
        public CacheItem<TVal>[] GetItems<TVal>()
        {
            return GetItemsInternal<TVal>(null);
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        public CacheItem<TVal>[] GetItems<TVal>(string partition)
        {
            return GetItemsInternal<TVal>(partition);
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<TVal> Peek<TVal>(string partition, string key)
        {
            return PeekInternal<TVal>(partition, key);
        }

        public FSharpOption<TVal> Peek<TVal>(string key)
        {
            return PeekInternal<TVal>(Settings.DefaultPartition, key);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<CacheItem<TVal>> PeekItem<TVal>(string partition, string key)
        {
            return PeekItemInternal<TVal>(partition, key);
        }

        public FSharpOption<CacheItem<TVal>> PeekItem<TVal>(string key)
        {
            return PeekItemInternal<TVal>(Settings.DefaultPartition, key);
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <returns>All values, without updating expiry dates.</returns>
        public CacheItem<TVal>[] PeekItems<TVal>()
        {
            return PeekItemsInternal<TVal>(null);
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        public CacheItem<TVal>[] PeekItems<TVal>(string partition)
        {
            return PeekItemsInternal<TVal>(partition);
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        public void Remove(string partition, string key)
        {
            RemoveInternal(partition, key);
        }

        public void Remove(string key)
        {
            RemoveInternal(Settings.DefaultPartition, key);
        }

        #endregion ICache Members

        #region Public Methods

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
        public int Count(string partition, CacheReadMode cacheReadMode)
        {
            return Convert.ToInt32(CountInternal(partition, cacheReadMode));
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
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

        #region Private Methods

        private void AddInternal<TVal>(string partition, string key, TVal value, DateTime? utcExpiry, TimeSpan? interval)
        {
            // Serializing may be pretty expensive, therefore we keep it out of the transaction.
            byte[] serializedValue;
            try
            {
                using (var memoryStream = new MemoryStream(1024))
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
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("serializedValue", serializedValue, DbType.Binary, size: serializedValue.Length);
            p.Add("utcExpiry", utcExpiry.HasValue ? utcExpiry.Value.ToUnixTime() : new long?(), DbType.Int64);
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

        private void ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
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

        private bool ContainsInternal(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource.ExecuteScalar<int>(SQLiteQueries.Contains, p) > 0;
            }
        }

        private long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
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

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        private FSharpOption<TVal> GetInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return DeserializeValue<TVal>(ctx.InternalResource.ExecuteScalar<byte[]>(SQLiteQueries.GetOne, p));
            }
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        private FSharpOption<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var tmp = ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.GetOneItem, p, buffered: false).FirstOrDefault();
                return DeserializeCacheItem<TVal>(tmp);
            }
        }

        private CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource
                    .Query<DbCacheItem>(SQLiteQueries.GetManyItems, p, buffered: false)
                    .Select(DeserializeCacheItem<TVal>)
                    .Where(i => i.HasValue())
                    .Select(i => i.Value)
                    .ToArray();
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
        private FSharpOption<TVal> PeekInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return DeserializeValue<TVal>(ctx.InternalResource.ExecuteScalar<byte[]>(SQLiteQueries.PeekOne, p));
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
        private FSharpOption<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                var tmp = ctx.InternalResource.Query<DbCacheItem>(SQLiteQueries.PeekOneItem, p, buffered: false).FirstOrDefault();
                return DeserializeCacheItem<TVal>(tmp);
            }
        }

        private CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("utcNow", _clock.UtcNow.ToUnixTime(), DbType.Int64);

            using (var ctx = _connectionPool.GetObject())
            {
                return ctx.InternalResource
                    .Query<DbCacheItem>(SQLiteQueries.PeekManyItems, p, buffered: false)
                    .Select(DeserializeCacheItem<TVal>)
                    .Where(i => i.HasValue())
                    .Select(i => i.Value)
                    .ToArray();
            }
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        private void RemoveInternal(string partition, string key)
        {
            var p = new DynamicParameters();
            p.Add("partition", partition, DbType.String);
            p.Add("key", key, DbType.String);

            using (var ctx = _connectionPool.GetObject())
            {
                ctx.InternalResource.Execute(SQLiteQueries.Remove, p);
            }
        }

        private TVal UnsafeDeserializeValue<TVal>(byte[] serializedValue)
        {
            using (var memoryStream = new MemoryStream(serializedValue))
            using (var decompressionStream = _compressor.CreateDecompressionStream(memoryStream))
            {
                return _serializer.DeserializeFromStream<TVal>(decompressionStream);
            }
        }

        private FSharpOption<TVal> DeserializeValue<TVal>(byte[] serializedValue)
        {
            if (serializedValue == null)
            {
                // Nothing to deserialize, return None.
                return FSharpOption<TVal>.None;
            }
            try
            {
                return UnsafeDeserializeValue<TVal>(serializedValue).Some();
            }
            catch (Exception ex)
            {
                // Something wrong happened during deserialization. Therefore, we act as if there
                // was no value and we return None.
                _log.Warn("Something wrong happened during deserialization", ex);
                return FSharpOption<TVal>.None;
            }
        }

        private FSharpOption<CacheItem<TVal>> DeserializeCacheItem<TVal>(DbCacheItem src)
        {
            if (src == null)
            {
                // Nothing to deserialize, return None.
                return FSharpOption<CacheItem<TVal>>.None;
            }
            try
            {
                return new CacheItem<TVal>
                {
                    Partition = src.Partition,
                    Key = src.Key,
                    Value = UnsafeDeserializeValue<TVal>(src.SerializedValue),
                    UtcCreation = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcCreation),
                    UtcExpiry = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcExpiry),
                    Interval = src.Interval == null ? new TimeSpan?() : TimeSpan.FromSeconds(src.Interval.Value)
                }.Some();
            }
            catch (Exception ex)
            {
                // Something wrong happened during deserialization. Therefore, we act as if there
                // was no element and we return None.
                _log.Warn("Something wrong happened during deserialization", ex);
                return FSharpOption<CacheItem<TVal>>.None;
            }
        }

        private PooledObjectWrapper<SQLiteConnection> CreatePooledConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
            var walAutoCheckpointInPages = journalSizeLimitInBytes / PageSizeInBytes / 3;
            var pragmas = string.Format(SQLiteQueries.SetPragmas, PageSizeInBytes, journalSizeLimitInBytes, walAutoCheckpointInPages);
            connection.Execute(pragmas);
            return new PooledObjectWrapper<SQLiteConnection>(connection);
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

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
        private sealed class DbCacheItem : EquatableObject<DbCacheItem>
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
