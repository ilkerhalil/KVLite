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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Common.IO;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An SQLite-based in-memory cache.
    /// </summary>
    /// <remarks>SQLite-based caches do not allow more than ten parent keys per item.</remarks>
    public sealed class VolatileCache : AbstractSQLiteCache<VolatileCacheSettings>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default application settings.
        /// </summary>
        [Pure]
#pragma warning disable CC0022 // Should dispose object
        public static VolatileCache DefaultInstance { get; } = new VolatileCache(new VolatileCacheSettings());
#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="VolatileCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="log">The log.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="memoryStreamManager">The memory stream manager.</param>
        public VolatileCache(VolatileCacheSettings settings, IClock clock = null, ILog log = null, ISerializer serializer = null, ICompressor compressor = null, IMemoryStreamManager memoryStreamManager = null)
            : base(settings, clock, log, serializer, compressor, memoryStreamManager)
        {
        }

        #endregion Construction

        #region AbstractCache Members

        /// <summary>
        ///   Returns whether the changed property is the data source.
        /// </summary>
        /// <param name="changedPropertyName">Name of the changed property.</param>
        /// <returns>Whether the changed property is the data source.</returns>
        protected override bool DataSourceHasChanged(string changedPropertyName)
        {
            return changedPropertyName.ToLower().Equals("cachename");
        }

        /// <summary>
        ///   Gets the data source, that is, the location of the SQLite store (it may be a file path
        ///   or a memory URI).
        /// </summary>
        /// <param name="journalMode">The journal mode.</param>
        /// <returns>The SQLite data source that will be used by the cache.</returns>
        protected override string GetDataSource(out SQLiteJournalModeEnum journalMode)
        {
            journalMode = SQLiteJournalModeEnum.Off;
            return string.Format("file:{0}?mode=memory&cache=shared", Settings.CacheName);
        }

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return KeyValuePair.Create("CacheName", Settings.CacheName);
        }

        #endregion AbstractCache Members
    }
}
