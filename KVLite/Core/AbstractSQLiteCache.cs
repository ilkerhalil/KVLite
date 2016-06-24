// File name: AbstractSQLiteCache.cs
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

using CodeProject.ObjectPool;
using Common.Logging;
using Finsa.CodeServices.Caching;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Common.IO.RecyclableMemoryStream;
using Finsa.CodeServices.Common.Portability;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using Ionic.Zlib;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;

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
        ///   The default SQLite page size in bytes. Do not change this value unless SQLite changes
        ///   its defaults. WAL journal does limit the capability to change that value even when the
        ///   DB is still empty.
        /// </summary>
        private const int PageSizeInBytes = 4096;

        /// <summary>
        ///   The string used to tag streams coming from <see cref="RecyclableMemoryStreamManager.Instance"/>.
        /// </summary>
        private const string StreamTag = nameof(KVLite);

        /// <summary>
        ///   The initial capacity of the streams retrieved from <see cref="RecyclableMemoryStreamManager.Instance"/>.
        /// </summary>
        private const int InitialStreamCapacity = 512;

        #endregion Constants

        #region Fields

        /// <summary>
        ///   The connection pool used to cache open connections.
        /// </summary>
        private ObjectPool<DbInterface> _connectionPool;

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

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
            Raise.ArgumentNullException.IfIsNull(settings, nameof(settings), ErrorMessages.NullSettings);

            _settings = settings;
            _clock = clock ?? new SystemClock();
            _log = log ?? LogManager.GetLogger(GetType());
            _compressor = compressor ?? new DeflateCompressor(CompressionLevel.BestSpeed);

            // We need to properly customize the default serializer settings in no custom serializer
            // has been specified.
            if (serializer != null)
            {
                // Use the specified serializer.
                _serializer = serializer;
            }
            else
            {
                // We apply many customizations to the JSON serializer, in order to achieve a small
                // output size.
                var serializerSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
                    DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.IgnoreAndPopulate,
                    Encoding = PortableEncoding.UTF8WithoutBOM,
                    FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.String,
                    Formatting = Newtonsoft.Json.Formatting.None,
                    MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
                    NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
                    PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None,
                    ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
                    TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
                };
                _serializer = new JsonSerializer(serializerSettings);
            }

            _settings.PropertyChanged += Settings_PropertyChanged;

            // Connection string must be customized by each cache.
            InitConnectionString();

            using (var db = _connectionPool.GetObject())
            using (var cmd = db.Connection.CreateCommand())
            {
                bool isSchemaReady;
                cmd.CommandText = SQLiteQueries.IsSchemaReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsSchemaReady(dataReader);
                }
                if (!isSchemaReady)
                {
                    // Creates the ICacheItem table and the required indexes.
                    cmd.CommandText = SQLiteQueries.CacheSchema;
                    cmd.ExecuteNonQuery();
                }
            }

            // Initial cleanup.
            ClearInternal(null, CacheReadMode.ConsiderExpiryDate);
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
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                // No need for a transaction, since it is just a select.
                using (var db = _connectionPool.GetObject())
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = "PRAGMA page_count;";
                    var pageCount = (long) cmd.ExecuteScalar();

                    cmd.CommandText = "PRAGMA freelist_count;";
                    var freelistCount = (long) cmd.ExecuteScalar();

                    cmd.CommandText = "PRAGMA page_size;";
                    var pageSizeInKB = (long) cmd.ExecuteScalar() / 1024L;

                    return (pageCount - freelistCount) * pageSizeInKB;
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnReadAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears the cache using the specified cache read mode.
        /// </summary>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear(CacheReadMode cacheReadMode)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = ClearInternal(null, cacheReadMode);

                // Postconditions - NOT VALID: Methods below return counters which are not related to
                // the number of items the call above actually cleared.

                //Debug.Assert(Count(cacheReadMode) == 0);
                //Debug.Assert(LongCount(cacheReadMode) == 0L);
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Clears the specified partition using the specified cache read mode.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear(string partition, CacheReadMode cacheReadMode)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = ClearInternal(partition, cacheReadMode);

                // Postconditions - NOT VALID: Methods below return counters which are not related to
                // the number of items the call above actually cleared.

                //Debug.Assert(Count(cacheReadMode) == 0);
                //Debug.Assert(LongCount(cacheReadMode) == 0L);
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnClearPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public int Count(CacheReadMode cacheReadMode)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = Convert.ToInt32(CountInternal(null, cacheReadMode));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0;
            }
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
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = Convert.ToInt32(CountInternal(partition, cacheReadMode));

                // Postconditions
                Debug.Assert(result >= 0);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0;
            }
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <param name="cacheReadMode">Whether invalid items should be included in the count.</param>
        /// <returns>The number of items in the cache.</returns>
        [Pure]
        public long LongCount(CacheReadMode cacheReadMode)
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = CountInternal(null, cacheReadMode);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnCountAll, ex);
                return 0L;
            }
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
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            RaiseArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = CountInternal(partition, cacheReadMode);

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0L;
            }
        }

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            // Preconditions
            RaiseObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

            try
            {
                _log.Info($"Vacuuming the SQLite DB '{Settings.CacheUri}'...");

                // Perform a cleanup before vacuuming.
                ClearInternal(null, CacheReadMode.ConsiderExpiryDate);

                // Vacuum cannot be run within a transaction.
                using (var db = _connectionPool.GetObject())
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = SQLiteQueries.Vacuum;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnVacuum, ex);
            }
        }

        #endregion Public Members

        #region IDisposable members

        /// <summary>
        ///   Performs application-defined tasks associated with freeing, releasing, or resetting
        ///   unmanaged resources.
        /// </summary>
        /// <param name="disposing">True if it is a managed dispose, false otherwise.</param>
        protected override void Dispose(bool disposing)
        {
            if (!disposing)
            {
                // Nothing to do, we can handle only managed Dispose calls.
                return;
            }

            if (_connectionPool != null)
            {
                _connectionPool.Clear();
                _connectionPool = null;
            }
        }

        #endregion IDisposable members

        #region ICache Members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>The clock used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public sealed override IClock Clock => _clock;

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="DeflateCompressor"/>.
        /// </remarks>
        public sealed override ICompressor Compressor => _compressor;

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to what
        ///   <see cref="LogManager.GetLogger(System.Type)"/> returns.
        /// </remarks>
        public sealed override ILog Log => _log;

        /// <summary>
        ///   The maximum number of parent keys each item can have. SQLite based caches support up to
        ///   five parent keys per item.
        /// </summary>
        public sealed override int MaxParentKeyCountPerItem { get; } = 5;

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>. Therefore,
        ///   if you do not specify another serializer, make sure that your objects are serializable
        ///   (in most cases, simply use the <see cref="SerializableAttribute"/> and expose fields as
        ///   public properties).
        /// </remarks>
        public sealed override ISerializer Serializer => _serializer;

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public sealed override TCacheSettings Settings => _settings;

        /// <summary>
        ///   <c>true</c> if the Peek methods are implemented, <c>false</c> otherwise.
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

        /// <summary>
        ///   Adds given value with the specified expiry time and refresh internal.
        /// </summary>
        /// <typeparam name="TVal">The type of the value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry time.</param>
        /// <param name="interval">The refresh interval.</param>
        /// <param name="parentKeys">
        ///   Keys, belonging to current partition, on which the new item will depend.
        /// </param>
        protected sealed override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            // Serializing may be pretty expensive, therefore we keep it out of the connection.
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
                LastError = ex;
                _log.ErrorFormat("Could not serialize given value '{0}'", ex, value.SafeToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            long insertionCount;
            using (var db = _connectionPool.GetObject())
            {
                db.Add_Partition.Value = partition;
                db.Add_Key.Value = key;
                db.Add_SerializedValue.Value = serializedValue;
                db.Add_UtcExpiry.Value = utcExpiry.ToUnixTime();
                db.Add_Interval.Value = (long) interval.TotalSeconds;
                db.Add_UtcNow.Value = _clock.UnixTime;
                db.Add_MaxInsertionCount.Value = Settings.InsertionCountBeforeAutoClean;

                // Also add the parent keys, if any.
                var parentKeyCount = parentKeys?.Count ?? 0;
                if (parentKeyCount != 0)
                {
                    db.Add_ParentKey0.Value = parentKeyCount > 0 ? parentKeys[0] : null;
                    db.Add_ParentKey1.Value = parentKeyCount > 1 ? parentKeys[1] : null;
                    db.Add_ParentKey2.Value = parentKeyCount > 2 ? parentKeys[2] : null;
                    db.Add_ParentKey3.Value = parentKeyCount > 3 ? parentKeys[3] : null;
                    db.Add_ParentKey4.Value = parentKeyCount > 4 ? parentKeys[4] : null;
                }
                else
                {
                    db.Add_ParentKey0.Value = null;
                    db.Add_ParentKey1.Value = null;
                    db.Add_ParentKey2.Value = null;
                    db.Add_ParentKey3.Value = null;
                    db.Add_ParentKey4.Value = null;
                }

                insertionCount = (long) db.Add_Command.ExecuteScalar();

                // We have to clear the serialized value parameter, in order to free some memory up.
                db.Add_SerializedValue.Value = null;
            }

            // Insertion has concluded successfully, therefore we increment the operation counter. If
            // it has reached the "InsertionCountBeforeAutoClean" configuration parameter, then we
            // must reset it and do a SOFT cleanup. Following code is not fully thread safe, but it
            // does not matter, because the "InsertionCountBeforeAutoClean" parameter should be just
            // an hint on when to do the cleanup.
            if (insertionCount >= Settings.InsertionCountBeforeAutoClean)
            {
                // If they were equal, then we need to run the maintenance cleanup. The insertion
                // counter is automatically reset by the Clear method.
                ClearInternal(null, CacheReadMode.ConsiderExpiryDate);
            }
        }

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected sealed override long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.Clear_Partition.Value = partition;
                db.Clear_IgnoreExpiryDate.Value = (cacheReadMode == CacheReadMode.IgnoreExpiryDate);
                db.Clear_UtcNow.Value = _clock.UnixTime;
                var removedItemCount = db.Clear_Command.ExecuteNonQuery();

                // Now we should perform a quick incremental vacuum.
                db.IncrementalVacuum_Command.ExecuteNonQuery();

                if (removedItemCount > 0 && partition == null)
                {
                    // If we are performing a full cache cleanup, then we also remove the items
                    // related to KVLite cache variables. Therefore, we have to remove the count of
                    // that variables from the number of deleted rows.
                    return removedItemCount - 1;
                }
                // In this case, the number is OK and can be returned.
                return removedItemCount;
            }
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected sealed override bool ContainsInternal(string partition, string key)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.Contains_Partition.Value = partition;
                db.Contains_Key.Value = key;
                db.Contains_UtcNow.Value = _clock.UnixTime;
                return (long) db.Contains_Command.ExecuteScalar() > 0L;
            }
        }

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected sealed override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.Count_Partition.Value = partition;
                db.Count_IgnoreExpiryDate.Value = (cacheReadMode == CacheReadMode.IgnoreExpiryDate);
                db.Count_UtcNow.Value = _clock.UnixTime;
                return (long) db.Count_Command.ExecuteScalar();
            }
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected sealed override Option<TVal> GetInternal<TVal>(string partition, string key)
        {
            byte[] serializedValue;
            using (var db = _connectionPool.GetObject())
            {
                db.GetOne_Partition.Value = partition;
                db.GetOne_Key.Value = key;
                db.GetOne_UtcNow.Value = _clock.UnixTime;
                serializedValue = (byte[]) db.GetOne_Command.ExecuteScalar();
            }

            return DeserializeValue<TVal>(serializedValue, partition, key);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected sealed override Option<ICacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            DbCacheItem tmpItem;
            using (var db = _connectionPool.GetObject())
            {
                db.GetOneItem_Partition.Value = partition;
                db.GetOneItem_Key.Value = key;
                db.GetOneItem_UtcNow.Value = _clock.UnixTime;
                using (var reader = db.GetOneItem_Command.ExecuteReader())
                {
                    tmpItem = MapDataReader(reader).FirstOrDefault();
                }
            }

            return DeserializeICacheItem<TVal>(tmpItem);
        }

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected sealed override ICacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.GetManyItems_Partition.Value = partition;
                db.GetManyItems_UtcNow.Value = _clock.UnixTime;
                using (var reader = db.GetManyItems_Command.ExecuteReader())
                {
                    return MapDataReader(reader)
                        .ToArray()
                        .Select(DeserializeICacheItem<TVal>)
                        .Where(i => i.HasValue)
                        .Select(i => i.Value)
                        .ToArray();
                }
            }
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        protected sealed override Option<TVal> PeekInternal<TVal>(string partition, string key)
        {
            byte[] serializedValue;
            using (var db = _connectionPool.GetObject())
            {
                db.PeekOne_Partition.Value = partition;
                db.PeekOne_Key.Value = key;
                db.PeekOne_UtcNow.Value = _clock.UnixTime;
                serializedValue = (byte[]) db.PeekOne_Command.ExecuteScalar();
            }

            return DeserializeValue<TVal>(serializedValue, partition, key);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        protected sealed override Option<ICacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            DbCacheItem tmpItem;
            using (var db = _connectionPool.GetObject())
            {
                db.PeekOneItem_Partition.Value = partition;
                db.PeekOneItem_Key.Value = key;
                db.PeekOneItem_UtcNow.Value = _clock.UnixTime;
                using (var reader = db.PeekOneItem_Command.ExecuteReader())
                {
                    tmpItem = MapDataReader(reader).FirstOrDefault();
                }
            }

            return DeserializeICacheItem<TVal>(tmpItem);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="T:System.Object"/> as type parameter; that will work whether the required
        ///   value is a class or not.
        /// </remarks>
        protected sealed override ICacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.PeekManyItems_Partition.Value = partition;
                db.PeekManyItems_UtcNow.Value = _clock.UnixTime;
                using (var reader = db.PeekManyItems_Command.ExecuteReader())
                {
                    return MapDataReader(reader)
                        .ToArray()
                        .Select(DeserializeICacheItem<TVal>)
                        .Where(i => i.HasValue)
                        .Select(i => i.Value)
                        .ToArray();
                }
            }
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected sealed override void RemoveInternal(string partition, string key)
        {
            using (var db = _connectionPool.GetObject())
            {
                db.Remove_Partition.Value = partition;
                db.Remove_Key.Value = key;
                db.Remove_Command.ExecuteNonQuery();
            }
        }

        private TVal UnsafeDeserializeValue<TVal>(byte[] serializedValue)
        {
            using (var memoryStream = RecyclableMemoryStreamManager.Instance.GetStream(StreamTag, serializedValue, 0, serializedValue.Length))
            using (var decompressionStream = _compressor.CreateDecompressionStream(memoryStream))
            {
                return _serializer.DeserializeFromStream<TVal>(decompressionStream);
            }
        }

        private Option<TVal> DeserializeValue<TVal>(byte[] serializedValue, string partition, string key)
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
                LastError = ex;
                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(partition, key);
                _log.Warn("Something wrong happened during deserialization", ex);
                return Option.None<TVal>();
            }
        }

        private Option<ICacheItem<TVal>> DeserializeICacheItem<TVal>(DbCacheItem src)
        {
            if (src == null)
            {
                // Nothing to deserialize, return None.
                return Option.None<ICacheItem<TVal>>();
            }
            try
            {
                return Option.Some<ICacheItem<TVal>>(new CacheItem<TVal>
                {
                    Partition = src.Partition,
                    Key = src.Key,
                    Value = UnsafeDeserializeValue<TVal>(src.SerializedValue),
                    UtcCreation = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcCreation),
                    UtcExpiry = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcExpiry),
                    Interval = TimeSpan.FromSeconds(src.Interval),
                    ParentKeys = src.ParentKeys
                });
            }
            catch (Exception ex)
            {
                LastError = ex;
                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(src.Partition, src.Key);
                _log.Warn("Something wrong happened during deserialization", ex);
                return Option.None<ICacheItem<TVal>>();
            }
        }

        private static IEnumerable<DbCacheItem> MapDataReader(SQLiteDataReader dataReader)
        {
            const int valueCount = 16;
            var values = new object[valueCount];

            while (dataReader.Read())
            {
                dataReader.GetValues(values);
                var dbICacheItem = new DbCacheItem
                {
                    Partition = values[0] as string,
                    Key = values[1] as string,
                    SerializedValue = values[2] as byte[],
                    UtcCreation = (long) values[3],
                    UtcExpiry = (long) values[4],
                    Interval = (long) values[5]
                };

                // Quickly read the parent keys, if any.
                const int parentKeysStartIndex = 6;
                var firstNullIndex = parentKeysStartIndex;
                while (firstNullIndex < valueCount && !(values[firstNullIndex] is DBNull)) { ++firstNullIndex; }
                var parentKeyCount = firstNullIndex - parentKeysStartIndex;
                if (parentKeyCount == 0)
                {
                    dbICacheItem.ParentKeys = CacheExtensions.NoParentKeys;
                }
                else
                {
                    dbICacheItem.ParentKeys = new string[parentKeyCount];
                    Array.Copy(values, parentKeysStartIndex, dbICacheItem.ParentKeys, 0, dbICacheItem.ParentKeys.Length);
                }

                yield return dbICacheItem;
            }
        }

        private DbInterface CreateDbInterface()
        {
#pragma warning disable CC0022 // Should dispose object

            // Create and open the connection.
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();

#pragma warning restore CC0022 // Should dispose object

            // Sets PRAGMAs for this new connection.
            using (var cmd = connection.CreateCommand())
            {
                var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
                cmd.CommandText = string.Format(SQLiteQueries.SetPragmas, journalSizeLimitInBytes);
                cmd.ExecuteNonQuery();
            }

#pragma warning disable CC0022 // Should dispose object

            return new DbInterface(connection);

#pragma warning restore CC0022 // Should dispose object
        }

        private void InitConnectionString()
        {
            SQLiteJournalModeEnum journalMode;
            var cacheUri = GetDataSource(out journalMode);

            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                FullUri = cacheUri,
                JournalMode = journalMode,
                FailIfMissing = false,
                LegacyFormat = false,
                ReadOnly = false,
                SyncMode = SynchronizationModes.Off,
                Version = 3,

                /* KVLite uses UNIX time */
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,

                /* Settings three minutes as timeout should be more than enough... */
                DefaultTimeout = 180,
                PrepareRetries = 3,

                /* Transaction handling */
                Enlist = false,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,

                /* Required by parent keys */
                ForeignKeys = true,
                RecursiveTriggers = true,

                /* Each page is 4KB large - Multiply by 1024*1024/PageSizeInBytes */
                MaxPageCount = Settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes,
                PageSize = PageSizeInBytes,
                CacheSize = -2000,

                /* We use a custom object pool */
                Pooling = false,
            };

            _connectionString = builder.ToString();
            _connectionPool = new ObjectPool<DbInterface>(1, 10, CreateDbInterface);
        }

        private void Settings_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (DataSourceHasChanged(e.PropertyName))
            {
                InitConnectionString();
            }
        }

        private static bool IsSchemaReady(SQLiteDataReader dataReader)
        {
            var columns = new HashSet<string>();

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 11
                && columns.Contains("partition")
                && columns.Contains("key")
                && columns.Contains("serializedValue")
                && columns.Contains("utcCreation")
                && columns.Contains("utcExpiry")
                && columns.Contains("interval")
                && columns.Contains("parentKey0")
                && columns.Contains("parentKey1")
                && columns.Contains("parentKey2")
                && columns.Contains("parentKey3")
                && columns.Contains("parentKey4");
        }

        #endregion Private Methods

        #region Nested type: DbInterface

        private sealed class DbInterface : PooledObject
        {
            public DbInterface(SQLiteConnection connection)
            {
                Connection = connection;

                // Add
                Add_Command = connection.CreateCommand();
                Add_Command.CommandText = SQLiteQueries.Add;
                Add_Command.Parameters.Add(Add_Partition = new SQLiteParameter("partition"));
                Add_Command.Parameters.Add(Add_Key = new SQLiteParameter("key"));
                Add_Command.Parameters.Add(Add_SerializedValue = new SQLiteParameter("serializedValue"));
                Add_Command.Parameters.Add(Add_UtcExpiry = new SQLiteParameter("utcExpiry"));
                Add_Command.Parameters.Add(Add_Interval = new SQLiteParameter("interval"));
                Add_Command.Parameters.Add(Add_UtcNow = new SQLiteParameter("utcNow"));
                Add_Command.Parameters.Add(Add_MaxInsertionCount = new SQLiteParameter("maxInsertionCount"));
                Add_Command.Parameters.Add(Add_ParentKey0 = new SQLiteParameter("parentKey0"));
                Add_Command.Parameters.Add(Add_ParentKey1 = new SQLiteParameter("parentKey1"));
                Add_Command.Parameters.Add(Add_ParentKey2 = new SQLiteParameter("parentKey2"));
                Add_Command.Parameters.Add(Add_ParentKey3 = new SQLiteParameter("parentKey3"));
                Add_Command.Parameters.Add(Add_ParentKey4 = new SQLiteParameter("parentKey4"));

                // Clear
                Clear_Command = Connection.CreateCommand();
                Clear_Command.CommandText = SQLiteQueries.Clear;
                Clear_Command.Parameters.Add(Clear_Partition = new SQLiteParameter("partition"));
                Clear_Command.Parameters.Add(Clear_IgnoreExpiryDate = new SQLiteParameter("ignoreExpiryDate"));
                Clear_Command.Parameters.Add(Clear_UtcNow = new SQLiteParameter("utcNow"));

                // Contains
                Contains_Command = Connection.CreateCommand();
                Contains_Command.CommandText = SQLiteQueries.Contains;
                Contains_Command.Parameters.Add(Contains_Partition = new SQLiteParameter("partition"));
                Contains_Command.Parameters.Add(Contains_Key = new SQLiteParameter("key"));
                Contains_Command.Parameters.Add(Contains_UtcNow = new SQLiteParameter("utcNow"));

                // Count
                Count_Command = Connection.CreateCommand();
                Count_Command.CommandText = SQLiteQueries.Count;
                Count_Command.Parameters.Add(Count_Partition = new SQLiteParameter("partition"));
                Count_Command.Parameters.Add(Count_IgnoreExpiryDate = new SQLiteParameter("ignoreExpiryDate"));
                Count_Command.Parameters.Add(Count_UtcNow = new SQLiteParameter("utcNow"));

                // GetOne
                GetOne_Command = Connection.CreateCommand();
                GetOne_Command.CommandText = SQLiteQueries.GetOne;
                GetOne_Command.Parameters.Add(GetOne_Partition = new SQLiteParameter("partition"));
                GetOne_Command.Parameters.Add(GetOne_Key = new SQLiteParameter("key"));
                GetOne_Command.Parameters.Add(GetOne_UtcNow = new SQLiteParameter("utcNow"));

                // GetOneItem
                GetOneItem_Command = Connection.CreateCommand();
                GetOneItem_Command.CommandText = SQLiteQueries.GetOneItem;
                GetOneItem_Command.Parameters.Add(GetOneItem_Partition = new SQLiteParameter("partition"));
                GetOneItem_Command.Parameters.Add(GetOneItem_Key = new SQLiteParameter("key"));
                GetOneItem_Command.Parameters.Add(GetOneItem_UtcNow = new SQLiteParameter("utcNow"));

                // GetManyItems
                GetManyItems_Command = Connection.CreateCommand();
                GetManyItems_Command.CommandText = SQLiteQueries.GetManyItems;
                GetManyItems_Command.Parameters.Add(GetManyItems_Partition = new SQLiteParameter("partition"));
                GetManyItems_Command.Parameters.Add(GetManyItems_UtcNow = new SQLiteParameter("utcNow"));

                // PeekOne
                PeekOne_Command = Connection.CreateCommand();
                PeekOne_Command.CommandText = SQLiteQueries.PeekOne;
                PeekOne_Command.Parameters.Add(PeekOne_Partition = new SQLiteParameter("partition"));
                PeekOne_Command.Parameters.Add(PeekOne_Key = new SQLiteParameter("key"));
                PeekOne_Command.Parameters.Add(PeekOne_UtcNow = new SQLiteParameter("utcNow"));

                // PeekOneItem
                PeekOneItem_Command = Connection.CreateCommand();
                PeekOneItem_Command.CommandText = SQLiteQueries.PeekOneItem;
                PeekOneItem_Command.Parameters.Add(PeekOneItem_Partition = new SQLiteParameter("partition"));
                PeekOneItem_Command.Parameters.Add(PeekOneItem_Key = new SQLiteParameter("key"));
                PeekOneItem_Command.Parameters.Add(PeekOneItem_UtcNow = new SQLiteParameter("utcNow"));

                // PeekManyItems
                PeekManyItems_Command = Connection.CreateCommand();
                PeekManyItems_Command.CommandText = SQLiteQueries.PeekManyItems;
                PeekManyItems_Command.Parameters.Add(PeekManyItems_Partition = new SQLiteParameter("partition"));
                PeekManyItems_Command.Parameters.Add(PeekManyItems_UtcNow = new SQLiteParameter("utcNow"));

                // Remove
                Remove_Command = Connection.CreateCommand();
                Remove_Command.CommandText = SQLiteQueries.Remove;
                Remove_Command.Parameters.Add(Remove_Partition = new SQLiteParameter("partition"));
                Remove_Command.Parameters.Add(Remove_Key = new SQLiteParameter("key"));

                // Vacuum
                IncrementalVacuum_Command = Connection.CreateCommand();
                IncrementalVacuum_Command.CommandText = SQLiteQueries.IncrementalVacuum;
            }

            protected override void OnReleaseResources()
            {
                Add_Command.Dispose();
                Clear_Command.Dispose();
                Contains_Command.Dispose();
                Count_Command.Dispose();
                GetOne_Command.Dispose();
                GetOneItem_Command.Dispose();
                GetManyItems_Command.Dispose();
                PeekOne_Command.Dispose();
                PeekOneItem_Command.Dispose();
                PeekManyItems_Command.Dispose();
                Remove_Command.Dispose();
                IncrementalVacuum_Command.Dispose();
                Connection.Dispose();

                base.OnReleaseResources();
            }

            public SQLiteConnection Connection { get; }

            #region Add

            public SQLiteCommand Add_Command { get; }
            public SQLiteParameter Add_Partition { get; }
            public SQLiteParameter Add_Key { get; }
            public SQLiteParameter Add_SerializedValue { get; }
            public SQLiteParameter Add_UtcExpiry { get; }
            public SQLiteParameter Add_Interval { get; }
            public SQLiteParameter Add_UtcNow { get; }
            public SQLiteParameter Add_MaxInsertionCount { get; }
            public SQLiteParameter Add_ParentKey0 { get; }
            public SQLiteParameter Add_ParentKey1 { get; }
            public SQLiteParameter Add_ParentKey2 { get; }
            public SQLiteParameter Add_ParentKey3 { get; }
            public SQLiteParameter Add_ParentKey4 { get; }

            #endregion Add

            #region Clear

            public SQLiteCommand Clear_Command { get; }
            public SQLiteParameter Clear_Partition { get; }
            public SQLiteParameter Clear_IgnoreExpiryDate { get; }
            public SQLiteParameter Clear_UtcNow { get; }

            #endregion Clear

            #region Contains

            public SQLiteCommand Contains_Command { get; }
            public SQLiteParameter Contains_Partition { get; }
            public SQLiteParameter Contains_Key { get; }
            public SQLiteParameter Contains_UtcNow { get; }

            #endregion Contains

            #region Count

            public SQLiteCommand Count_Command { get; }
            public SQLiteParameter Count_Partition { get; }
            public SQLiteParameter Count_IgnoreExpiryDate { get; }
            public SQLiteParameter Count_UtcNow { get; }

            #endregion Count

            #region GetOne

            public SQLiteCommand GetOne_Command { get; }
            public SQLiteParameter GetOne_Partition { get; }
            public SQLiteParameter GetOne_Key { get; }
            public SQLiteParameter GetOne_UtcNow { get; }

            #endregion GetOne

            #region GetOneItem

            public SQLiteCommand GetOneItem_Command { get; }
            public SQLiteParameter GetOneItem_Partition { get; }
            public SQLiteParameter GetOneItem_Key { get; }
            public SQLiteParameter GetOneItem_UtcNow { get; }

            #endregion GetOneItem

            #region GetManyItems

            public SQLiteCommand GetManyItems_Command { get; }
            public SQLiteParameter GetManyItems_Partition { get; }
            public SQLiteParameter GetManyItems_UtcNow { get; }

            #endregion GetManyItems

            #region PeekOne

            public SQLiteCommand PeekOne_Command { get; }
            public SQLiteParameter PeekOne_Partition { get; }
            public SQLiteParameter PeekOne_Key { get; }
            public SQLiteParameter PeekOne_UtcNow { get; }

            #endregion PeekOne

            #region PeekOneItem

            public SQLiteCommand PeekOneItem_Command { get; }
            public SQLiteParameter PeekOneItem_Partition { get; }
            public SQLiteParameter PeekOneItem_Key { get; }
            public SQLiteParameter PeekOneItem_UtcNow { get; }

            #endregion PeekOneItem

            #region PeekManyItems

            public SQLiteCommand PeekManyItems_Command { get; }
            public SQLiteParameter PeekManyItems_Partition { get; }
            public SQLiteParameter PeekManyItems_UtcNow { get; }

            #endregion PeekManyItems

            #region Remove

            public SQLiteCommand Remove_Command { get; }
            public SQLiteParameter Remove_Partition { get; }
            public SQLiteParameter Remove_Key { get; }

            #endregion Remove

            #region Vacuum

            public SQLiteCommand IncrementalVacuum_Command { get; }

            #endregion Vacuum
        }

        #endregion Nested type: DbInterface

        #region Nested type: DbCacheItem

        /// <summary>
        ///   Represents a row in the cache table.
        /// </summary>
        [Serializable]
        private sealed class DbCacheItem : EquatableObject<DbCacheItem>
        {
            #region Public Properties

            public string Partition { get; set; }

            public string Key { get; set; }

            public byte[] SerializedValue { get; set; }

            public long UtcCreation { get; set; }

            public long UtcExpiry { get; set; }

            public long Interval { get; set; }

            public string[] ParentKeys { get; set; }

            #endregion Public Properties

            #region EquatableObject<DbICacheItem> Members

            /// <summary>
            ///   Returns all property (or field) values, along with their names, so that they can be
            ///   used to produce a meaningful <see cref="M:Finsa.CodeServices.Common.FormattableObject.ToString"/>.
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

            #endregion EquatableObject<DbICacheItem> Members
        }

        #endregion Nested type: DbCacheItem
    }
}