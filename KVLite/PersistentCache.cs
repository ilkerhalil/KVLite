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

using System;
using System.Collections.Generic;
using System.IO;
using Common.Logging;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class PersistentCache : CacheBase<PersistentCache, PersistentCacheSettings>
    {
        #region Construction

        static PersistentCache()
        {
            // Makes SQLite work... (loading dll from e.g. KVLite/x64/SQLite.Interop.dll)
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", nativePath);

            // Logs the path where SQLite has been set.
            LogManager.GetLogger<PersistentCache>().InfoFormat("SQLite native libraries set at {0}", nativePath);
        }

        /// <summary>
        ///   TODO
        /// </summary>
        public PersistentCache()
            : base(new PersistentCacheSettings())
        {
        }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="settings"></param>
        public PersistentCache(PersistentCacheSettings settings)
            : base(settings)
        {
        }

        #endregion Construction

        #region CacheBase Members

        protected override bool DataSourceHasChanged(string changedPropertyName)
        {
            return changedPropertyName.ToLower().Equals("cachefile");
        }

        protected override string GetDataSource()
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

            return mappedPath;
        }

        protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return GKeyValuePair.Create("CacheFile", Settings.CacheFile);
        }

        #endregion
    }
}