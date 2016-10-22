// File name: VolatileCache.cs
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

using CodeProject.ObjectPool.Specialized;
using PommaLabs.CodeServices.Clock;
using PommaLabs.CodeServices.Compression;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.SQLite;
using System;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An SQLite-based in-memory cache.
    /// </summary>
    /// <remarks>SQLite-based caches do not allow more than ten parent keys per item.</remarks>
    public sealed class VolatileCache : DbCache<VolatileCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default cache settings.
        /// </summary>
        [Pure]
#pragma warning disable CC0022 // Should dispose object

        public static VolatileCache DefaultInstance { get; } = new VolatileCache(new VolatileCacheSettings());

#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        #region Fields

        /// <summary>
        ///   Since in-memory SQLite instances are deleted as soon as the connection is closed, then
        ///   we keep one dangling connection open, so that the store does not disappear.
        /// </summary>
        private IDbConnection _keepAliveConnection;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="VolatileCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamPool">The memory stream pool.</param>
        public VolatileCache(VolatileCacheSettings settings, IClock clock = null, ISerializer serializer = null, ICompressor compressor = null, IMemoryStreamPool memoryStreamPool = null)
            : base(settings, new SQLiteCacheConnectionFactory<VolatileCacheSettings>(settings, SQLiteJournalModeEnum.Memory), clock, serializer, compressor, memoryStreamPool)
        {
            // Connection string must be customized by each cache.
            UpdateConnectionString();

            Settings.PropertyChanged += (sender, args) =>
            {
                if (DataSourceHasChanged(args.PropertyName))
                {
                    UpdateConnectionString();
                }
            };
        }

        #endregion Construction

        #region Private members

        private void UpdateConnectionString()
        {
            var sqliteConnFactory = (ConnectionFactory as SQLiteCacheConnectionFactory<VolatileCacheSettings>);
            var dataSource = GetDataSource(Settings.CacheName);
            sqliteConnFactory.InitConnectionString(dataSource);
            sqliteConnFactory.EnsureSchemaIsReady();

            _keepAliveConnection?.Dispose();
            _keepAliveConnection = sqliteConnFactory.Create();
            _keepAliveConnection.Open();
        }

        /// <summary>
        ///   Returns whether the changed property is the data source.
        /// </summary>
        /// <param name="changedPropertyName">Name of the changed property.</param>
        /// <returns>Whether the changed property is the data source.</returns>
        private static bool DataSourceHasChanged(string changedPropertyName) => string.Equals(changedPropertyName, nameof(VolatileCacheSettings.CacheName), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        ///   Gets the data source, that is, the location of the SQLite store (it might be a file
        ///   path or a memory URI).
        /// </summary>
        /// <param name="cacheName">User specified cache name.</param>
        /// <returns>The SQLite data source that will be used by the cache.</returns>
        private static string GetDataSource(string cacheName) => string.Format("file:{0}?mode=memory&cache=shared", cacheName);

        #endregion Private members
    }
}