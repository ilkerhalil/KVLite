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
using CodeProject.ObjectPool.Specialized;
using Common.Logging;
using LinqToDB;
using PommaLabs.CodeServices.Caching;
using PommaLabs.CodeServices.Clock;
using PommaLabs.CodeServices.Common;
using PommaLabs.CodeServices.Compression;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class AbstractSQLiteCache<TSettings> : AbstractCache<TSettings>
        where TSettings : AbstractSQLiteCacheSettings<TSettings>
    {
        #region Constants

        /// <summary>
        ///   The default SQLite page size in bytes. Do not change this value unless SQLite changes
        ///   its defaults. WAL journal does limit the capability to change that value even when the
        ///   DB is still empty.
        /// </summary>
        private const int PageSizeInBytes = 4096;

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
        private readonly TSettings _settings;

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
        /// <param name="dbCacheConnectionFactory">The DB connection factory.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamPool">The memory stream pool.</param>
        internal AbstractSQLiteCache(TSettings settings, IDbCacheConnectionFactory dbCacheConnectionFactory, IClock clock, ILog log, ISerializer serializer, ICompressor compressor, IMemoryStreamPool memoryStreamPool)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(settings, nameof(settings), ErrorMessages.NullSettings);

            _settings = settings;
            ConnectionFactory = dbCacheConnectionFactory;
            _log = log ?? LogManager.GetLogger(GetType());
            _compressor = compressor ?? Constants.DefaultCompressor;
            _serializer = serializer ?? Constants.DefaultSerializer;
            MemoryStreamPool = memoryStreamPool ?? CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance;
            _clock = clock ?? Constants.DefaultClock;

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
            if (ConnectionFactory != null)
            {
                ClearInternal(null, CacheReadMode.ConsiderExpiryDate);
            }
        }

        #endregion Construction

        #region Public Members

        public IDbCacheConnectionFactory ConnectionFactory { get; set; }

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        [Pure]
        public long CacheSizeInKB()
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
            Raise.ArgumentException.IfNot(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);

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
        ///   The pool used to retrieve <see cref="MemoryStream"/> instances.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance"/>.
        /// </remarks>
        public sealed override IMemoryStreamPool MemoryStreamPool { get; }

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
        public sealed override TSettings Settings => _settings;

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
                using (var memoryStream = MemoryStreamPool.GetObject().MemoryStream)
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
                _log.ErrorFormat(ErrorMessages.InternalErrorOnSerializationFormat, ex, value.SafeToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            var dbCacheItem = new DbCacheItem
            {
                Id = DbCacheItem.Hash(partition, key),
                Partition = partition,
                Key = key,
                Value = serializedValue,
                UtcCreation = _clock.UnixTime,
                UtcExpiry = utcExpiry.ToUnixTime(),
                Interval = (long) interval.TotalSeconds
            };

            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                // At first, we try to update the item, since it usually exists. It is not necessary
                // to update PARTITION and KEY, since they must already have the proper values
                // (otherwise, hashes should not have matched).
                var updatedRows = db.CacheItems
                    .Where(x => x.Id == dbCacheItem.Id)
                    .Set(x => x.Value, dbCacheItem.Value)
                    .Set(x => x.UtcCreation, dbCacheItem.UtcCreation)
                    .Set(x => x.UtcExpiry, dbCacheItem.UtcExpiry)
                    .Set(x => x.Interval, dbCacheItem.Interval)
                    .Update();

                if (updatedRows == 0)
                {
                    try
                    {
                        db.Insert(dbCacheItem);
                    }
#pragma warning disable CC0004 // Catch block cannot be empty
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
                    catch
                    {
                        // Insert will fail if item already exists, but we do not care.
                    }
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning restore CC0004 // Catch block cannot be empty
                }
            }

            long insertionCount = 0L;
            using (var db = _connectionPool.GetObject())
            {
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
            // Compute all parameters _before_ opening the connection.
            var ignoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate);
            var utcNow = _clock.UnixTime;

            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                return db.CacheItems
                    .Where(x => partition == null || x.Partition == partition)
                    .Where(x => ignoreExpiryDate || x.UtcExpiry < utcNow)
                    .Delete();
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
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);
            var utcNow = _clock.UnixTime;

            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                // Search for at least one valid item.
                return db.CacheItems.Any(x => x.Id == dbCacheItemId && x.UtcExpiry >= utcNow);
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
            // Compute all parameters _before_ opening the connection.
            var ignoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate);
            var utcNow = _clock.UnixTime;

            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                return db.CacheItems
                    .Where(x => partition == null || x.Partition == partition)
                    .Where(x => ignoreExpiryDate || x.UtcExpiry >= utcNow)
                    .LongCount();
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
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);
            var utcNow = _clock.UnixTime;

            byte[] serializedValue;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                var dbCacheItem = db.CacheItems
                    .Where(x => x.Id == dbCacheItemId && x.UtcExpiry >= utcNow)
                    .Select(x => new { x.Value, OldUtcExpiry = x.UtcExpiry, NewUtcExpiry = utcNow + x.Interval })
                    .FirstOrDefault();

                if (dbCacheItem == null)
                {
                    // Nothing to deserialize, return None.
                    return Option.None<TVal>();
                }

                // Since we are in a "get" operation, we should also update the expiry.
                if (dbCacheItem.NewUtcExpiry > utcNow)
                {
                    db.CacheItems
                        .Where(x => x.Id == dbCacheItemId && x.UtcExpiry == dbCacheItem.OldUtcExpiry)
                        .Set(x => x.UtcExpiry, dbCacheItem.NewUtcExpiry)
                        .Update();
                }

                serializedValue = dbCacheItem.Value;
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
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
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);
            var utcNow = _clock.UnixTime;

            DbCacheItem dbCacheItem;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                dbCacheItem = db.CacheItems
                    .FirstOrDefault(x => x.Id == dbCacheItemId && x.UtcExpiry >= utcNow);

                if (dbCacheItem == null)
                {
                    // Nothing to deserialize, return None.
                    return Option.None<ICacheItem<TVal>>();
                }

                // Since we are in a "get" operation, we should also update the expiry.
                if (dbCacheItem.Interval > 0L)
                {
                    db.CacheItems
                        .Where(x => x.Id == dbCacheItemId && x.UtcExpiry == dbCacheItem.UtcExpiry)
                        .Set(x => x.UtcExpiry, utcNow + dbCacheItem.Interval)
                        .Update();
                }
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheItem<TVal>(dbCacheItem);
        }

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected sealed override IList<ICacheItem<TVal>> GetItemsInternal<TVal>(string partition)
        {
            // Compute all parameters _before_ opening the connection.
            var utcNow = _clock.UnixTime;

            DbCacheItem[] dbCacheItems;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                dbCacheItems = db.CacheItems
                    .Where(x => partition == null || x.Partition == partition)
                    .Where(x => x.UtcExpiry >= utcNow)
                    .ToArray();

                // Since we are in a "get" operation, we should also update the expiry.
                foreach (var dbCacheItem in dbCacheItems.Where(x => x.Interval > 0L))
                {
                    db.CacheItems
                        .Where(x => x.Id == dbCacheItem.Id && x.UtcExpiry == dbCacheItem.UtcExpiry)
                        .Set(x => x.UtcExpiry, utcNow + dbCacheItem.Interval)
                        .Update();
                }
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return dbCacheItems
                .Select(DeserializeCacheItem<TVal>)
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToArray();
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
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);
            var utcNow = _clock.UnixTime;

            byte[] serializedValue;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                var dbCacheItem = db.CacheItems
                    .Where(x => x.Id == dbCacheItemId && x.UtcExpiry >= utcNow)
                    .Select(x => new { x.Value, OldUtcExpiry = x.UtcExpiry, NewUtcExpiry = utcNow + x.Interval })
                    .FirstOrDefault();

                if (dbCacheItem == null)
                {
                    // Nothing to deserialize, return None.
                    return Option.None<TVal>();
                }

                serializedValue = dbCacheItem.Value;
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
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
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);
            var utcNow = _clock.UnixTime;

            DbCacheItem dbCacheItem;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                dbCacheItem = db.CacheItems
                    .FirstOrDefault(x => x.Id == dbCacheItemId && x.UtcExpiry >= utcNow);

                if (dbCacheItem == null)
                {
                    // Nothing to deserialize, return None.
                    return Option.None<ICacheItem<TVal>>();
                }
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheItem<TVal>(dbCacheItem);
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
        protected sealed override IList<ICacheItem<TVal>> PeekItemsInternal<TVal>(string partition)
        {
            // Compute all parameters _before_ opening the connection.
            var utcNow = _clock.UnixTime;

            DbCacheItem[] dbCacheItems;
            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                dbCacheItems = db.CacheItems
                    .Where(x => partition == null || x.Partition == partition)
                    .Where(x => x.UtcExpiry >= utcNow)
                    .ToArray();
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return dbCacheItems
                .Select(DeserializeCacheItem<TVal>)
                .Where(i => i.HasValue)
                .Select(i => i.Value)
                .ToArray();
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected sealed override void RemoveInternal(string partition, string key)
        {
            // Compute all parameters _before_ opening the connection.
            var dbCacheItemId = DbCacheItem.Hash(partition, key);

            using (var db = new DbCacheConnection(ConnectionFactory))
            {
                db.CacheItems
                    .Where(x => x.Id == dbCacheItemId)
                    .Delete();
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

        private Option<TVal> DeserializeValue<TVal>(byte[] serializedValue, string partition, string key)
        {
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

                _log.Warn(ErrorMessages.InternalErrorOnDeserialization, ex);

                return Option.None<TVal>();
            }
        }

        private Option<ICacheItem<TVal>> DeserializeCacheItem<TVal>(DbCacheItem src)
        {
            try
            {
                return Option.Some<ICacheItem<TVal>>(new CacheItem<TVal>
                {
                    Partition = src.Partition,
                    Key = src.Key,
                    Value = UnsafeDeserializeValue<TVal>(src.Value),
                    UtcCreation = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcCreation),
                    UtcExpiry = DateTimeExtensions.UnixTimeStart.AddSeconds(src.UtcExpiry),
                    Interval = TimeSpan.FromSeconds(src.Interval),
                    //ParentKeys = src.ParentKeys
                });
            }
            catch (Exception ex)
            {
                LastError = ex;

                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(src.Partition, src.Key);

                _log.Warn(ErrorMessages.InternalErrorOnDeserialization, ex);

                return Option.None<ICacheItem<TVal>>();
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

                // Vacuum
                IncrementalVacuum_Command = Connection.CreateCommand();
                IncrementalVacuum_Command.CommandText = SQLiteQueries.IncrementalVacuum;
            }

            protected override void OnReleaseResources()
            {
                Add_Command.Dispose();
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

            #region Vacuum

            public SQLiteCommand IncrementalVacuum_Command { get; }

            #endregion Vacuum
        }

        #endregion Nested type: DbInterface     
    }
}