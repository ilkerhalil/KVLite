// File name: DbCache.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using CodeProject.ObjectPool.Specialized;
using Dapper;
using PommaLabs.KVLite.Database;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Logging;
using PommaLabs.Thrower;
using PommaLabs.Thrower.Goodies;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Troschuetz.Random;

#if !NET40

using System.Threading;
using System.Threading.Tasks;

#endif

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for SQL caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TSettings">The type of the cache settings.</typeparam>
    /// <typeparam name="TConnection">The type of the cache connection.</typeparam>
    public class DbCache<TSettings, TConnection> : AbstractCache<TSettings>
        where TSettings : DbCacheSettings<TSettings, TConnection>
        where TConnection : DbConnection
    {
        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="DbCache{TSettings, TConnection}"/> class
        ///   with given settings.
        /// </summary>
        /// <param name="settings">The settings.</param>
        /// <param name="connectionFactory">The DB connection factory.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamPool">The memory stream pool.</param>
        /// <param name="randomGenerator">The random number generator.</param>
        public DbCache(TSettings settings, IDbCacheConnectionFactory<TConnection> connectionFactory, IClock clock, ISerializer serializer, ICompressor compressor, IMemoryStreamPool memoryStreamPool, IGenerator randomGenerator)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(settings, nameof(settings), ErrorMessages.NullSettings);
            Raise.ArgumentNullException.IfIsNull(connectionFactory, nameof(connectionFactory));

            Settings = settings;
            Settings.ConnectionFactory = connectionFactory;
            Clock = clock ?? SystemClock.Instance;
            Serializer = serializer ?? JsonSerializer.Instance;
            Compressor = compressor ?? GZipCompressor.Instance;
            MemoryStreamPool = memoryStreamPool ?? CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance;
            RandomGenerator = randomGenerator ?? CacheConstants.CreateRandomGenerator();
        }

        #endregion Construction

        #region Public members

        /// <summary>
        ///   The connection factory used to retrieve connections to the cache data store.
        /// </summary>
        public IDbCacheConnectionFactory<TConnection> ConnectionFactory => Settings.ConnectionFactory;

        /// <summary>
        ///   Generates random numbers. Used to determine when to perform automatic soft cleanups.
        /// </summary>
        public IGenerator RandomGenerator { get; }

        /// <summary>
        ///   Clears the cache using the specified cache read mode.
        /// </summary>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        public long Clear(CacheReadMode cacheReadMode)
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

#if !NET40

        /// <summary>
        ///   Clears the cache using the specified cache read mode.
        /// </summary>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        public async Task<long> ClearAsync(CacheReadMode cacheReadMode, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Preconditions
            Raise.ObjectDisposedException.If(Disposed, nameof(ICache), ErrorMessages.CacheHasBeenDisposed);
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

            try
            {
                var result = await ClearAsyncInternal(null, cacheReadMode, cancellationToken).ConfigureAwait(false);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnClearAll, ex);
                return 0L;
            }
        }

#endif

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
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnClearPartition, partition), ex);
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
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
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
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
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
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(ErrorMessages.InternalErrorOnCountAll, ex);
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
            Raise.ArgumentException.IfIsNotValidEnum(cacheReadMode, nameof(cacheReadMode), ErrorMessages.InvalidCacheReadMode);

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
                Log.ErrorException(string.Format(ErrorMessages.InternalErrorOnCountPartition, partition), ex);
                return 0L;
            }
        }

        #endregion Public members

        #region FormattableObject members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="object.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="object.ToString"/>.
        /// </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return new KeyValuePair<string, string>(nameof(Settings.CacheUri), Settings.CacheUri);
        }

        #endregion FormattableObject members

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
        }

        #endregion IDisposable members

        #region ICache members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        public sealed override IClock Clock { get; }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="GZipCompressor"/>.
        /// </remarks>
        public sealed override ICompressor Compressor { get; }

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
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>.
        /// </remarks>
        public sealed override ISerializer Serializer { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public sealed override TSettings Settings { get; }

        /// <summary>
        ///   <c>true</c> if the Peek methods are implemented, <c>false</c> otherwise.
        /// </summary>
        public override bool CanPeek => true;

        #endregion ICache members

        #region Protected methods

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected sealed override long GetCacheSizeInBytesInternal()
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;

            using (var db = cf.Open())
            {
                return db.QueryFirstOrDefault<long?>(cf.GetCacheSizeInBytesQuery) ?? 0L;
            }
        }

#if !NET40

        /// <summary>
        ///   Computes cache size in bytes. This value might be an estimate of real cache size and,
        ///   therefore, it does not need to be extremely accurate.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An estimate of cache size in bytes.</returns>
        protected sealed override async Task<long> GetCacheSizeInBytesAsyncInternal(CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;

            using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return (await db.QueryFirstOrDefaultAsync<long?>(cf.GetCacheSizeInBytesQuery).ConfigureAwait(false)) ?? 0L;
            }
        }

#endif

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
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            partition = partition.Truncate(cf.MaxPartitionNameLength);
            key = key.Truncate(cf.MaxKeyNameLength);

            var dynamicParameters = PrepareCacheEntryForAdd(partition, key, value, utcExpiry, interval, parentKeys);

            ThrowOnFailedRetries(CacheConstants.RetryPolicy.ExecuteAndCapture(() =>
            {
                using (var db = cf.Open())
                using (var tr = db.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    db.Execute(cf.InsertOrUpdateCacheEntryCommand, dynamicParameters, tr);
                    tr.Commit();
                }
            }));

            if (RandomGenerator.NextDouble() < Settings.ChancesOfAutoCleanup)
            {
                // Run soft cleanup, so that cache is almost always clean. We do not call the
                // internal version since we need the following method not to throw anything in case
                // of error. A missed cleanup should not break the insertion.
                Clear(CacheReadMode.ConsiderExpiryDate);
            }
        }

#if !NET40

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
        /// <param name="cancellationToken">An optional cancellation token.</param>
        protected sealed override async Task AddAsyncInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys, CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            partition = partition.Truncate(cf.MaxPartitionNameLength);
            key = key.Truncate(cf.MaxKeyNameLength);

            var dynamicParameters = PrepareCacheEntryForAdd(partition, key, value, utcExpiry, interval, parentKeys);

            ThrowOnFailedRetries(await CacheConstants.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
                using (var tr = db.BeginTransaction(IsolationLevel.ReadCommitted))
                {
                    await db.ExecuteAsync(cf.InsertOrUpdateCacheEntryCommand, dynamicParameters, tr).ConfigureAwait(false);
                    tr.Commit();
                }
            }).ConfigureAwait(false));

            if (RandomGenerator.NextDouble() < Settings.ChancesOfAutoCleanup)
            {
                // Run soft cleanup, so that cache is almost always clean. We do not call the
                // internal version since we need the following method not to throw anything in case
                // of error. A missed cleanup should not break the insertion.
                await ClearAsync(CacheReadMode.ConsiderExpiryDate).ConfigureAwait(false);
            }
        }

#endif

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected sealed override long ClearInternal(string partition, CacheReadMode cacheReadMode)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate) ? DbCacheValue.True : DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            return ThrowOnFailedRetries(CacheConstants.RetryPolicy.ExecuteAndCapture(() =>
            {
                using (var db = cf.Open())
                {
                    return db.Execute(cf.DeleteCacheEntriesCommand, dbCacheEntryGroup);
                }
            }));
        }

#if !NET40

        /// <summary>
        ///   Clears this instance or a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items that have been removed.</returns>
        protected sealed override async Task<long> ClearAsyncInternal(string partition, CacheReadMode cacheReadMode, CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate) ? DbCacheValue.True : DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            return ThrowOnFailedRetries(await CacheConstants.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
                {
                    return await db.ExecuteAsync(cf.DeleteCacheEntriesCommand, dbCacheEntryGroup).ConfigureAwait(false);
                }
            }).ConfigureAwait(false));
        }

#endif

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
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                UtcExpiry = Clock.UnixTime()
            };

            using (var db = cf.Open())
            {
                // Search for at least one valid item.
                return db.QuerySingle<long>(cf.ContainsCacheEntryQuery, dbCacheEntrySingle) > 0L;
            }
        }

#if !NET40

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected sealed override async Task<bool> ContainsAsyncInternal(string partition, string key, CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                UtcExpiry = Clock.UnixTime()
            };

            using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                // Search for at least one valid item.
                return (await db.QuerySingleAsync<long>(cf.ContainsCacheEntryQuery, dbCacheEntrySingle).ConfigureAwait(false)) > 0L;
            }
        }

#endif

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected sealed override long CountInternal(string partition, CacheReadMode cacheReadMode)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate) ? DbCacheValue.True : DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            using (var db = cf.Open())
            {
                return db.QuerySingle<long>(cf.CountCacheEntriesQuery, dbCacheEntryGroup);
            }
        }

#if !NET40

        /// <summary>
        ///   The number of items in the cache or in a partition, if specified.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <param name="cacheReadMode">The cache read mode.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        /// <returns>The number of items in the cache.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        protected sealed override async Task<long> CountAsyncInternal(string partition, CacheReadMode cacheReadMode, CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = (cacheReadMode == CacheReadMode.IgnoreExpiryDate) ? DbCacheValue.True : DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
            {
                return await db.QuerySingleAsync<long>(cf.CountCacheEntriesQuery, dbCacheEntryGroup).ConfigureAwait(false);
            }
        }

#endif

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by the corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        protected sealed override CacheResult<TVal> GetInternal<TVal>(string partition, string key)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                IgnoreExpiryDate = DbCacheValue.True,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheValue dbCacheValue;
            using (var db = cf.Open())
            {
                dbCacheValue = db.QuerySingleOrDefault<DbCacheValue>(cf.PeekCacheValueQuery, dbCacheEntrySingle);

                if (dbCacheValue == null)
                {
                    // Nothing to deserialize, return None.
                    return default(CacheResult<TVal>);
                }

                if (dbCacheValue.UtcExpiry < dbCacheEntrySingle.UtcExpiry)
                {
                    // When an item expires, we should remove it from the cache.
                    db.Execute(cf.DeleteCacheEntryCommand, dbCacheEntrySingle);

                    // Nothing to deserialize, return None.
                    return default(CacheResult<TVal>);
                }

                if (dbCacheValue.Interval > 0L)
                {
                    // Since we are in a "get" operation, we should also update the expiry.
                    dbCacheEntrySingle.UtcExpiry += dbCacheValue.Interval;
                    db.Execute(cf.UpdateCacheEntryExpiryCommand, dbCacheEntrySingle);
                }
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheValue<TVal>(dbCacheValue);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        protected sealed override CacheResult<ICacheItem<TVal>> GetItemInternal<TVal>(string partition, string key)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                IgnoreExpiryDate = DbCacheValue.True,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheEntry dbCacheEntry;
            using (var db = cf.Open())
            {
                dbCacheEntry = db.QuerySingleOrDefault<DbCacheEntry>(cf.PeekCacheEntryQuery, dbCacheEntrySingle);

                if (dbCacheEntry == null)
                {
                    // Nothing to deserialize, return None.
                    return default(CacheResult<ICacheItem<TVal>>);
                }

                if (dbCacheEntry.UtcExpiry < dbCacheEntrySingle.UtcExpiry)
                {
                    // When an item expires, we should remove it from the cache.
                    db.Execute(cf.DeleteCacheEntryCommand, dbCacheEntrySingle);

                    // Nothing to deserialize, return None.
                    return default(CacheResult<ICacheItem<TVal>>);
                }

                if (dbCacheEntry.Interval > 0L)
                {
                    // Since we are in a "get" operation, we should also update the expiry.
                    dbCacheEntry.UtcExpiry = (dbCacheEntrySingle.UtcExpiry += dbCacheEntry.Interval);
                    db.Execute(cf.UpdateCacheEntryExpiryCommand, dbCacheEntrySingle);
                }
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheEntry<TVal>(dbCacheEntry);
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
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = DbCacheValue.True,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheEntry[] dbCacheEntries;
            using (var db = cf.Open())
            using (var tr = db.BeginTransaction(IsolationLevel.ReadCommitted))
            {
                dbCacheEntries = db.Query<DbCacheEntry>(cf.PeekCacheEntriesQuery, dbCacheEntryGroup, tr, false).ToArray();

                foreach (var dbCacheEntry in dbCacheEntries)
                {
                    if (dbCacheEntry.UtcExpiry < dbCacheEntryGroup.UtcExpiry)
                    {
                        // When an item expires, we should remove it from the cache.
                        db.Execute(cf.DeleteCacheEntryCommand, new DbCacheEntry.Single
                        {
                            Partition = dbCacheEntry.Partition,
                            Key = dbCacheEntry.Key
                        }, tr);
                    }
                    else if (dbCacheEntry.Interval > 0L)
                    {
                        // Since we are in a "get" operation, we should also update the expiry.
                        db.Execute(cf.UpdateCacheEntryExpiryCommand, new DbCacheEntry.Single
                        {
                            Partition = dbCacheEntry.Partition,
                            Key = dbCacheEntry.Key,
                            UtcExpiry = dbCacheEntry.UtcExpiry = dbCacheEntryGroup.UtcExpiry + dbCacheEntry.Interval
                        }, tr);
                    }
                }

                tr.Commit();
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return dbCacheEntries
                .Where(i => i.UtcExpiry >= dbCacheEntryGroup.UtcExpiry)
                .Select(DeserializeCacheEntry<TVal>)
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
        protected sealed override CacheResult<TVal> PeekInternal<TVal>(string partition, string key)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                IgnoreExpiryDate = DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheValue dbCacheValue;
            using (var db = cf.Open())
            {
                dbCacheValue = db.QuerySingleOrDefault<DbCacheValue>(cf.PeekCacheValueQuery, dbCacheEntrySingle);
            }

            if (dbCacheValue == null)
            {
                // Nothing to deserialize, return None.
                return default(CacheResult<TVal>);
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheValue<TVal>(dbCacheValue);
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
        protected sealed override CacheResult<ICacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength),
                IgnoreExpiryDate = DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheEntry dbCacheEntry;
            using (var db = cf.Open())
            {
                dbCacheEntry = db.QuerySingleOrDefault<DbCacheEntry>(cf.PeekCacheEntryQuery, dbCacheEntrySingle);
            }

            if (dbCacheEntry == null)
            {
                // Nothing to deserialize, return None.
                return default(CacheResult<ICacheItem<TVal>>);
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return DeserializeCacheEntry<TVal>(dbCacheEntry);
        }

        /// <summary>
        ///   Gets the all values in the cache or in the specified partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition">The optional partition.</param>
        /// <typeparam name="TVal">The type of the expected values.</typeparam>
        /// <returns>All values, without updating expiry dates.</returns>
        /// <remarks>
        ///   If you are uncertain of which type the value should have, you can always pass
        ///   <see cref="Object"/> as type parameter; that will work whether the required value is a
        ///   class or not.
        /// </remarks>
        protected sealed override IList<ICacheItem<TVal>> PeekItemsInternal<TVal>(string partition)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntryGroup = new DbCacheEntry.Group
            {
                Partition = partition?.Truncate(cf.MaxPartitionNameLength),
                IgnoreExpiryDate = DbCacheValue.False,
                UtcExpiry = Clock.UnixTime()
            };

            DbCacheEntry[] dbCacheEntries;
            using (var db = cf.Open())
            {
                dbCacheEntries = db.Query<DbCacheEntry>(cf.PeekCacheEntriesQuery, dbCacheEntryGroup, buffered: false).ToArray();
            }

            // Deserialize operation is expensive and it should be performed outside the connection.
            return dbCacheEntries
                .Select(DeserializeCacheEntry<TVal>)
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
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength)
            };

            ThrowOnFailedRetries(CacheConstants.RetryPolicy.ExecuteAndCapture(() =>
            {
                using (var db = cf.Open())
                {
                    db.Execute(cf.DeleteCacheEntryCommand, dbCacheEntrySingle);
                }
            }));
        }

#if !NET40

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="cancellationToken">An optional cancellation token.</param>
        protected sealed override async Task RemoveAsyncInternal(string partition, string key, CancellationToken cancellationToken)
        {
            // Compute all parameters _before_ opening the connection.
            var cf = Settings.ConnectionFactory;
            var dbCacheEntrySingle = new DbCacheEntry.Single
            {
                Partition = partition.Truncate(cf.MaxPartitionNameLength),
                Key = key.Truncate(cf.MaxKeyNameLength)
            };

            ThrowOnFailedRetries(await CacheConstants.AsyncRetryPolicy.ExecuteAndCaptureAsync(async () =>
            {
                using (var db = await cf.OpenAsync(cancellationToken).ConfigureAwait(false))
                {
                    await db.ExecuteAsync(cf.DeleteCacheEntryCommand, dbCacheEntrySingle).ConfigureAwait(false);
                }
            }).ConfigureAwait(false));
        }

#endif

        /// <summary>
        ///   Converts given cache entry into dynamic parameters.
        /// </summary>
        /// <param name="dbCacheEntry">Cache entry.</param>
        /// <returns>Given cache entry converted into dynamic parameters.</returns>
        protected virtual SqlMapper.IDynamicParameters ToDynamicParameters(DbCacheEntry dbCacheEntry) => new DynamicParameters(dbCacheEntry);

        #endregion Protected methods

        #region Serialization and deserialization

        private TVal UnsafeDeserializeCacheValue<TVal>(DbCacheValue dbCacheValue)
        {
            using (var memoryStream = new MemoryStream(dbCacheValue.Value))
            {
                if (dbCacheValue.Compressed == DbCacheValue.False)
                {
                    // Handle uncompressed value.
                    AntiTamper.ReadAntiTamperHashCode(memoryStream, dbCacheValue);
                    return BinarySerializer.Deserialize<TVal>(Serializer, MemoryStreamPool, memoryStream);
                }
                else
                {
                    // Handle compressed value.
                    using (var decompressionStream = Compressor.CreateDecompressionStream(memoryStream))
                    {
                        AntiTamper.ReadAntiTamperHashCode(decompressionStream, dbCacheValue);
                        return BinarySerializer.Deserialize<TVal>(Serializer, MemoryStreamPool, decompressionStream);
                    }
                }
            }
        }

        private CacheResult<TVal> DeserializeCacheValue<TVal>(DbCacheValue dbCacheValue)
        {
            try
            {
                return UnsafeDeserializeCacheValue<TVal>(dbCacheValue);
            }
            catch (Exception ex)
            {
                LastError = ex;

                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(dbCacheValue.Partition, dbCacheValue.Key);

                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex);

                return default(CacheResult<TVal>);
            }
        }

        private CacheResult<ICacheItem<TVal>> DeserializeCacheEntry<TVal>(DbCacheEntry dbCacheEntry)
        {
            try
            {
                var cacheItem = new CacheItem<TVal>
                {
                    Partition = dbCacheEntry.Partition,
                    Key = dbCacheEntry.Key,
                    Value = UnsafeDeserializeCacheValue<TVal>(dbCacheEntry),
                    UtcCreation = dbCacheEntry.UtcCreation.UtcDateTime(),
                    UtcExpiry = dbCacheEntry.UtcExpiry.UtcDateTime(),
                    Interval = TimeSpan.FromSeconds(dbCacheEntry.Interval)
                };

                // Quickly read the parent keys, if any.
                if (dbCacheEntry.ParentKey0 != null)
                {
                    if (dbCacheEntry.ParentKey1 != null)
                    {
                        if (dbCacheEntry.ParentKey2 != null)
                        {
                            if (dbCacheEntry.ParentKey3 != null)
                            {
                                if (dbCacheEntry.ParentKey4 != null)
                                {
                                    cacheItem.ParentKeys = new[] { dbCacheEntry.ParentKey0, dbCacheEntry.ParentKey1, dbCacheEntry.ParentKey2, dbCacheEntry.ParentKey3, dbCacheEntry.ParentKey4 };
                                }
                                else cacheItem.ParentKeys = new[] { dbCacheEntry.ParentKey0, dbCacheEntry.ParentKey1, dbCacheEntry.ParentKey2, dbCacheEntry.ParentKey3 };
                            }
                            else cacheItem.ParentKeys = new[] { dbCacheEntry.ParentKey0, dbCacheEntry.ParentKey1, dbCacheEntry.ParentKey2 };
                        }
                        else cacheItem.ParentKeys = new[] { dbCacheEntry.ParentKey0, dbCacheEntry.ParentKey1 };
                    }
                    else cacheItem.ParentKeys = new[] { dbCacheEntry.ParentKey0 };
                }
                else cacheItem.ParentKeys = CacheExtensions.NoParentKeys;

                return cacheItem;
            }
            catch (Exception ex)
            {
                LastError = ex;

                // Something wrong happened during deserialization. Therefore, we remove the old
                // element (in order to avoid future errors) and we return None.
                RemoveInternal(dbCacheEntry.Partition, dbCacheEntry.Key);

                Log.WarnException(ErrorMessages.InternalErrorOnDeserialization, ex);

                return default(CacheResult<ICacheItem<TVal>>);
            }
        }

        private SqlMapper.IDynamicParameters PrepareCacheEntryForAdd<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            var cf = Settings.ConnectionFactory;

            // Create the new entry here, before the serialization, so that we can use its hash code
            // to perform some anti-tamper checks when the value will be read.
            var dbCacheEntry = new DbCacheEntry
            {
                Partition = partition,
                Key = key,
                UtcExpiry = utcExpiry.UnixTime(),
                Interval = (long) interval.TotalSeconds,
                UtcCreation = Clock.UnixTime()
            };

            // Serializing may be pretty expensive, therefore we keep it out of the connection.
            try
            {
                using (var serializedStream = MemoryStreamPool.GetObject().MemoryStream)
                {
                    // First write the anti-tamper hash code...
                    AntiTamper.WriteAntiTamperHashCode(serializedStream, dbCacheEntry);

                    // Then serialize the new value.
                    BinarySerializer.Serialize(Serializer, serializedStream, value);

                    if (serializedStream.Length > Settings.MinValueLengthForCompression)
                    {
                        // Stream is too long, we should compress it.
                        using (var compressedStream = MemoryStreamPool.GetObject().MemoryStream)
                        {
                            using (var compressionStream = Compressor.CreateCompressionStream(compressedStream))
                            {
                                serializedStream.Position = 0L;
                                serializedStream.CopyTo(compressionStream);
                            }
                            dbCacheEntry.Value = compressedStream.ToArray();
                            dbCacheEntry.Compressed = DbCacheValue.True;
                        }
                    }
                    else
                    {
                        // Stream is shorter than specified threshold, we can store it as it is.
                        dbCacheEntry.Value = serializedStream.ToArray();
                        dbCacheEntry.Compressed = DbCacheValue.False;
                    }
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.ErrorException(ErrorMessages.InternalErrorOnSerializationFormat, ex, value?.ToString());
                throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
            }

            // Also add the parent keys, if any.
            var parentKeyCount = parentKeys?.Count ?? 0;
            if (parentKeyCount > 0)
            {
                dbCacheEntry.ParentKey0 = parentKeys[0].Truncate(cf.MaxKeyNameLength);
                if (parentKeyCount > 1)
                {
                    dbCacheEntry.ParentKey1 = parentKeys[1].Truncate(cf.MaxKeyNameLength);
                    if (parentKeyCount > 2)
                    {
                        dbCacheEntry.ParentKey2 = parentKeys[2].Truncate(cf.MaxKeyNameLength);
                        if (parentKeyCount > 3)
                        {
                            dbCacheEntry.ParentKey3 = parentKeys[3].Truncate(cf.MaxKeyNameLength);
                            if (parentKeyCount > 4)
                            {
                                dbCacheEntry.ParentKey4 = parentKeys[4].Truncate(cf.MaxKeyNameLength);
                            }
                        }
                    }
                }
            }

            return ToDynamicParameters(dbCacheEntry);
        }

        #endregion Serialization and deserialization

        #region Retry policy management

        [MethodImpl(CacheConstants.MethodImplOptions)]
        private static void ThrowOnFailedRetries(Polly.PolicyResult policyResult)
        {
            if (policyResult.FinalException != null)
            {
                throw policyResult.FinalException;
            }
        }

        [MethodImpl(CacheConstants.MethodImplOptions)]
        private static T ThrowOnFailedRetries<T>(Polly.PolicyResult<T> policyResult)
        {
            if (policyResult.FinalException != null)
            {
                throw policyResult.FinalException;
            }
            return policyResult.Result;
        }

        #endregion Retry policy management
    }
}
