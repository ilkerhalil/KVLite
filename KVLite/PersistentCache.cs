// File name: PersistentCache.cs
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

using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Utilities;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Contracts;
using System.IO;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An SQLite-based persistent cache.
    /// </summary>
    public sealed class PersistentCache : CacheBase<PersistentCache, PersistentCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   The default cache instance.
        /// </summary>
        private static readonly PersistentCache CachedDefaultInstance;

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        [Pure]
        public static PersistentCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        #endregion Default Instance

        #region Construction

        static PersistentCache()
        {
            InitSQLite();
            CachedDefaultInstance = new PersistentCache(new PersistentCacheSettings());
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="PersistentCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        public PersistentCache(PersistentCacheSettings settings, IClock clock = null, ILog log = null, ISerializer serializer = null, ICompressor compressor = null)
            : base(settings, clock, log, serializer, compressor)
        {
        }

        #endregion Construction

        #region CacheBase Members

        /// <summary>
        ///   Returns whether the changed property is the data source.
        /// </summary>
        /// <param name="changedPropertyName">Name of the changed property.</param>
        /// <returns>Whether the changed property is the data source.</returns>
        protected override bool DataSourceHasChanged(string changedPropertyName)
        {
            return changedPropertyName.ToLower().Equals("cachefile");
        }

        /// <summary>
        ///   Gets the data source, that is, the location of the SQLite store (it may be a file path
        ///   or a memory URI).
        /// </summary>
        /// <param name="journalMode">The journal mode.</param>
        /// <returns>The SQLite data source that will be used by the cache.</returns>
        protected override string GetDataSource(out SQLiteJournalModeEnum journalMode)
        {
            // Map cache path, since it may be an IIS relative path.
            var mappedPath = Settings.CacheFile.MapPath();

            // If the directory which should contain the cache does not exist, then we create it.
            // SQLite will take care of creating the DB itself.
            var cacheDir = Path.GetDirectoryName(mappedPath);
            if (cacheDir != null && !Directory.Exists(cacheDir))
            {
                Directory.CreateDirectory(cacheDir);
            }

            journalMode = SQLiteJournalModeEnum.Wal;
            return mappedPath;
        }

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </returns>
        protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return GKeyValuePair.Create("CacheFile", Settings.CacheFile);
        }

        #endregion CacheBase Members
    }
}