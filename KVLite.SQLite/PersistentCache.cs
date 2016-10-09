// File name: PersistentCache.cs
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
using Common.Logging;
using PommaLabs.CodeServices.Clock;
using PommaLabs.CodeServices.Common.Portability;
using PommaLabs.CodeServices.Compression;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.SQLite;
using System;
using System.Data.SQLite;
using System.Diagnostics.Contracts;
using System.IO;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An SQLite-based persistent cache.
    /// </summary>
    /// <remarks>SQLite-based caches do not allow more than ten parent keys per item.</remarks>
    public sealed class PersistentCache : DbCache<PersistentCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        [Pure]
#pragma warning disable CC0022 // Should dispose object

        public static PersistentCache DefaultInstance { get; } = new PersistentCache(new PersistentCacheSettings());

#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="PersistentCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamPool">The memory stream pool.</param>
        public PersistentCache(PersistentCacheSettings settings, IClock clock = null, ILog log = null, ISerializer serializer = null, ICompressor compressor = null, IMemoryStreamPool memoryStreamPool = null)
            : base(settings, new SQLiteCacheConnectionFactory<PersistentCacheSettings>(settings, SQLiteJournalModeEnum.Wal), clock, log, serializer, compressor, memoryStreamPool)
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

        #region Public members

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            try
            {
                Log.Info($"Vacuuming the SQLite DB '{Settings.CacheFile}'...");
                (ConnectionFactory as SQLiteCacheConnectionFactory<PersistentCacheSettings>).Vacuum();
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnVacuum, ex);
            }
        }

        #endregion

        #region Private members

        private void UpdateConnectionString()
        {
            var sqliteConnFactory = (ConnectionFactory as SQLiteCacheConnectionFactory<PersistentCacheSettings>);
            var dataSource = GetDataSource(Settings.CacheFile);
            sqliteConnFactory.InitConnectionString(dataSource);
            sqliteConnFactory.EnsureSchemaIsReady();
        }

        /// <summary>
        ///   Returns whether the changed property is the data source.
        /// </summary>
        /// <param name="changedPropertyName">Name of the changed property.</param>
        /// <returns>Whether the changed property is the data source.</returns>
        private static bool DataSourceHasChanged(string changedPropertyName) => string.Equals(changedPropertyName, nameof(PersistentCacheSettings.CacheFile), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        ///   Gets the data source, that is, the location of the SQLite store (it might be a file
        ///   path or a memory URI).
        /// </summary>
        /// <param name="cacheFile">User specified cache file.</param>
        /// <returns>The SQLite data source that will be used by the cache.</returns>
        private static string GetDataSource(string cacheFile)
        {
            // Map cache path, since it may be an IIS relative path.
            var mappedPath = PortableEnvironment.MapPath(cacheFile);

            // If the directory which should contain the cache does not exist, then we create it.
            // SQLite will take care of creating the DB itself.
            var cacheDir = Path.GetDirectoryName(mappedPath);
            if (cacheDir != null && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            return mappedPath;
        }

        #endregion Private members
    }
}