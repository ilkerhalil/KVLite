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
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Common.IO.RecyclableMemoryStream;
using Finsa.CodeServices.Common.Portability;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
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
        private ObjectPool<PooledObjectWrapper<SQLiteConnection>> _connectionPool;

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
            RaiseArgumentNullException.IfIsNull(settings, nameof(settings), ErrorMessages.NullSettings);

            _settings = settings;
            _clock = clock ?? new SystemClock();
            _log = log ?? LogManager.GetLogger(GetType());
            _compressor = compressor ?? new SnappyCompressor();

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
                    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Simple
                };
                _serializer = new JsonSerializer(serializerSettings);
            }

            _settings.PropertyChanged += Settings_PropertyChanged;

            // Connection string must be customized by each cache.
            InitConnectionString();

            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                bool isSchemaReady;
                cmd.CommandText = SQLiteQueries.IsSchemaReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsSchemaReady(dataReader);
                }
                if (!isSchemaReady)
                {
                    // Creates the CacheItem table and the required indexes.
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
                using (var ctx = _connectionPool.GetObject())
                using (var cmd = ctx.InternalResource.CreateCommand())
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
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
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
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
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
            RaiseArgumentNullException.IfIsNull(partition, nameof(partition), ErrorMessages.NullPartition);
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
                using (var ctx = _connectionPool.GetObject())
                using (var cmd = ctx.InternalResource.CreateCommand())
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
        ///   constructor. If not specified, it defaults to <see cref="SnappyCompressor"/>.
        /// </remarks>
        public sealed override ICompressor Compressor => _compressor;

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to what <see
        ///   cref="LogManager.GetLogger(System.Type)"/> returns.
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
                LastError = ex;
                _log.ErrorFormat("Could not serialize given value '{0}'", ex, value.SafeToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            long insertionCount;
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Add;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter(nameof(serializedValue), serializedValue),
                    new SQLiteParameter(nameof(utcExpiry), utcExpiry.ToUnixTime()),
                    new SQLiteParameter(nameof(interval), (long) interval.TotalSeconds),
                    new SQLiteParameter("utcNow", _clock.UnixTime),
                    new SQLiteParameter("maxInsertionCount", Settings.InsertionCountBeforeAutoClean)
                });

                // Also add the parent keys, if any.
                var parameters = cmd.Parameters;
                var parentKeyCount = parentKeys?.Count ?? 0;
                if (parentKeyCount != 0)
                {
                    parameters.Add(new SQLiteParameter("parentKey0", parentKeyCount > 0 ? parentKeys[0] : null));
                    parameters.Add(new SQLiteParameter("parentKey1", parentKeyCount > 1 ? parentKeys[1] : null));
                    parameters.Add(new SQLiteParameter("parentKey2", parentKeyCount > 2 ? parentKeys[2] : null));
                    parameters.Add(new SQLiteParameter("parentKey3", parentKeyCount > 3 ? parentKeys[3] : null));
                    parameters.Add(new SQLiteParameter("parentKey4", parentKeyCount > 4 ? parentKeys[4] : null));
                }
                else
                {
                    parameters.Add(new SQLiteParameter("parentKey0", null));
                    parameters.Add(new SQLiteParameter("parentKey1", null));
                    parameters.Add(new SQLiteParameter("parentKey2", null));
                    parameters.Add(new SQLiteParameter("parentKey3", null));
                    parameters.Add(new SQLiteParameter("parentKey4", null));
                }

                insertionCount = (long) cmd.ExecuteScalar();
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
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Clear;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate)),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                var removedItemCount = cmd.ExecuteNonQuery();

                // Now we should perform a quick incremental vacuum.
                cmd.CommandText = SQLiteQueries.IncrementalVacuum;
                cmd.Parameters.Clear();
                cmd.ExecuteNonQuery();

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
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Contains;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                return (long) cmd.ExecuteScalar() > 0L;
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
            // No need for a transaction, since it is just a select.
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Count;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter("ignoreExpiryDate", (cacheReadMode == CacheReadMode.IgnoreExpiryDate)),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                return (long) cmd.ExecuteScalar();
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
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.GetOne;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                serializedValue = (byte[]) cmd.ExecuteScalar();
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
        protected sealed override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            DbCacheItem tmpItem;
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.GetOneItem;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                using (var reader = cmd.ExecuteReader())
                {
                    tmpItem = MapDataReader(reader).FirstOrDefault();
                }
            }

            return DeserializeCacheItem<TVal>(tmpItem);
        }

        /// <summary>
        ///   Gets all cache items or the ones in a partition, if specified. If an item is a
        ///   "sliding" or "static" value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All cache items.</returns>
        protected sealed override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.GetManyItems;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                using (var reader = cmd.ExecuteReader())
                {
                    return MapDataReader(reader)
                        .ToArray()
                        .Select(DeserializeCacheItem<TVal>)
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
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.PeekOne;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                serializedValue = (byte[]) cmd.ExecuteScalar();
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
        protected sealed override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            DbCacheItem tmpItem;
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.PeekOneItem;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                using (var reader = cmd.ExecuteReader())
                {
                    tmpItem = MapDataReader(reader).FirstOrDefault();
                }
            }

            return DeserializeCacheItem<TVal>(tmpItem);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass <see
        ///   cref="T:System.Object"/> as type parameter; that will work whether the required value
        ///   is a class or not.
        /// </remarks>
        protected sealed override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.PeekManyItems;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter("utcNow", _clock.UnixTime)
                });
                using (var reader = cmd.ExecuteReader())
                {
                    return MapDataReader(reader)
                        .ToArray()
                        .Select(DeserializeCacheItem<TVal>)
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
            using (var ctx = _connectionPool.GetObject())
            using (var cmd = ctx.InternalResource.CreateCommand())
            {
                cmd.CommandText = SQLiteQueries.Remove;
                cmd.Parameters.AddRange(new[]
                {
                    new SQLiteParameter(nameof(partition), partition),
                    new SQLiteParameter(nameof(key), key)
                });
                cmd.ExecuteNonQuery();
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

        private Option<CacheItem<TVal>> DeserializeCacheItem<TVal>(DbCacheItem src)
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
                return Option.None<CacheItem<TVal>>();
            }
        }

        private static IEnumerable<DbCacheItem> MapDataReader(SQLiteDataReader dataReader)
        {
            const int valueCount = 16;
            var values = new object[valueCount];

            while (dataReader.Read())
            {
                dataReader.GetValues(values);
                var dbCacheItem = new DbCacheItem
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
                    dbCacheItem.ParentKeys = DbCacheItem.NoParentKeys;
                }
                else
                {
                    dbCacheItem.ParentKeys = new string[parentKeyCount];
                    Array.Copy(values, parentKeysStartIndex, dbCacheItem.ParentKeys, 0, dbCacheItem.ParentKeys.Length);
                }

                yield return dbCacheItem;
            }
        }

        private PooledObjectWrapper<SQLiteConnection> CreatePooledConnection()
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
                var walAutoCheckpointInPages = journalSizeLimitInBytes / PageSizeInBytes / 3;
                cmd.CommandText = string.Format(SQLiteQueries.SetPragmas, journalSizeLimitInBytes, walAutoCheckpointInPages);
                cmd.ExecuteNonQuery();
            }

#pragma warning disable CC0022 // Should dispose object

            var pooledConnection = new PooledObjectWrapper<SQLiteConnection>(connection);
            pooledConnection.WrapperReleaseResourcesAction += (sc => sc.Dispose());
            return pooledConnection;

#pragma warning restore CC0022 // Should dispose object
        }

        private void InitConnectionString()
        {
            SQLiteJournalModeEnum journalMode;
            var cacheUri = GetDataSource(out journalMode);

            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                /* Number of pages of 1KB, 2000 is the suggested value */
                CacheSize = -2000,
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,
                /* Settings three minutes as timeout should be more than enough... */
                DefaultTimeout = 180,
                Enlist = false,
                FailIfMissing = false,
                /* Required by parent keys */
                ForeignKeys = true,
                FullUri = cacheUri,
                JournalMode = journalMode,
                LegacyFormat = false,
                /* Each page is 1KB large - Multiply by 1024*1024/PageSizeInBytes */
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

        #region Nested type: DbCacheItem

        /// <summary>
        ///   Represents a row in the cache table.
        /// </summary>
        [Serializable]
        private sealed class DbCacheItem : EquatableObject<DbCacheItem>
        {
            #region Constants

            public static readonly string[] NoParentKeys = new string[0];

            #endregion Constants

            #region Public Properties

            public string Partition { get; set; }

            public string Key { get; set; }

            public byte[] SerializedValue { get; set; }

            public long UtcCreation { get; set; }

            public long UtcExpiry { get; set; }

            public long Interval { get; set; }

            public string[] ParentKeys { get; set; }

            #endregion Public Properties

            #region EquatableObject<DbCacheItem> Members

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

            #endregion EquatableObject<DbCacheItem> Members
        }

        #endregion Nested type: DbCacheItem
    }
}