// File name: IDbCacheConnectionFactory.cs
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

using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Creates new connections to a specified SQL provider.
    /// </summary>
    public interface IDbCacheConnectionFactory
    {
        string CacheSchemaName { get; set; }

        string CacheItemsTableName { get; set; }

        string CacheValuesTableName { get; set; }

        int MaxPartitionNameLength { get; }

        int MaxKeyNameLength { get; }

        #region Commands

        string InsertOrUpdateCacheEntryCommand { get; }

        string DeleteCacheEntryCommand { get; }

        string DeleteCacheEntriesCommand { get; }

        #endregion Commands

        #region Queries

        string ContainsCacheEntryQuery { get; }

        string CountCacheEntriesQuery { get; }

        string PeekCacheEntryQuery { get; }

        string PeekCacheValueQuery { get; }

        #endregion

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        string ConnectionString { get; set; }

        /// <summary>
        ///   Creates a new connection to the specified data provider.
        /// </summary>
        /// <returns>A connection which might be opened.</returns>
        DbConnection Create();

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <returns>An open connection.</returns>
        DbConnection Open();

        /// <summary>
        ///   Opens a new connection to the specified data provider.
        /// </summary>
        /// <param name="cancellationToken">The cancellation instruction.</param>
        /// <returns>An open connection.</returns>
        Task<DbConnection> OpenAsync(CancellationToken cancellationToken);

        /// <summary>
        ///   Returns current cache size in kilobytes.
        /// </summary>
        /// <returns>Current cache size in kilobytes.</returns>
        long GetCacheSizeInBytes();
    }
}