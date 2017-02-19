// File name: IDbCacheConnectionFactory.cs
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

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Creates new connections to a specified SQL provider.
    /// </summary>
    /// <typeparam name="TConnection">The type of the cache connection.</typeparam>
    public interface IDbCacheConnectionFactory<TConnection>
        where TConnection : DbConnection
    {
        #region Configuration

        /// <summary>
        ///   The schema which holds cache entries table.
        /// </summary>
        string CacheSchemaName { get; set; }

        /// <summary>
        ///   The name of the table which holds cache entries.
        /// </summary>
        string CacheEntriesTableName { get; set; }

        /// <summary>
        ///   The maximum length a partition can have. Longer partitions will be truncated.
        /// </summary>
        int MaxPartitionNameLength { get; }

        /// <summary>
        ///   The maximum length a key can have. Longer keys will be truncated.
        /// </summary>
        int MaxKeyNameLength { get; }

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        string ConnectionString { get; set; }

        #endregion Configuration

        #region Commands

        /// <summary>
        ///   Inserts or updates a cache entry.
        /// </summary>
        string InsertOrUpdateCacheEntryCommand { get; }

        /// <summary>
        ///   Deletes a cache entry using specified hash.
        /// </summary>
        string DeleteCacheEntryCommand { get; }

        /// <summary>
        ///   Deletes a set of cache entries.
        /// </summary>
        string DeleteCacheEntriesCommand { get; }

        /// <summary>
        ///   Updates the expiry time of one cache entry.
        /// </summary>
        string UpdateCacheEntryExpiryCommand { get; }

        #endregion Commands

        #region Queries

        /// <summary>
        ///   Checks whether a valid entry with given hash exists.
        /// </summary>
        string ContainsCacheEntryQuery { get; }

        /// <summary>
        ///   Counts valid or invalid cache entries.
        /// </summary>
        string CountCacheEntriesQuery { get; }

        /// <summary>
        ///   Returns all valid cache entries without updating their expiry time.
        /// </summary>
        string PeekCacheEntriesQuery { get; }

        /// <summary>
        ///   Returns a valid cache entry with given hash.
        /// </summary>
        string PeekCacheEntryQuery { get; }

        /// <summary>
        ///   Returns a valid cache value with given hash.
        /// </summary>
        string PeekCacheValueQuery { get; }

        /// <summary>
        ///   Returns current cache size in bytes.
        /// </summary>
        string GetCacheSizeInBytesQuery { get; }

        #endregion Queries

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <returns>An open connection.</returns>
        TConnection Open();

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        Task<TConnection> OpenAsync(CancellationToken cancellationToken);
    }
}
