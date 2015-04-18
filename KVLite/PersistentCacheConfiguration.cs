// File name: PersistentCacheConfiguration.cs
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

using Westwind.Utilities.Configuration;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Configuration class for the <see cref="PersistentCache"/>.
    /// </summary>
    public sealed class PersistentCacheConfiguration : AppConfiguration
    {
        #region Static instance

        private static readonly PersistentCacheConfiguration CachedInstance;

        static PersistentCacheConfiguration()
        {
            CachedInstance = new PersistentCacheConfiguration();
            CachedInstance.Initialize();
        }

        /// <summary>
        ///   Gets the static configuration instance.
        /// </summary>
        /// <value>The static configuration instance.</value>
        public static PersistentCacheConfiguration Instance
        {
            get { return CachedInstance; }
        }

        #endregion Static instance

        /// <summary>
        ///   Initializes a new instance of the <see cref="PersistentCacheConfiguration"/> class and
        ///   it sets the default values for this configuration.
        /// </summary>
        public PersistentCacheConfiguration()
        {
            DefaultCacheFile = "PersistentCache.sqlite";
            DefaultPartition = "KVLite.DefaultPartition";
            DefaultStaticIntervalInDays = 30;
            DefaultInsertionCountBeforeAutoClean = 64;
            DefaultMaxCacheSizeInMB = 1024;
            DefaultMaxJournalSizeInMB = 32;
        }

        /// <summary>
        ///   Gets or sets the default cache file, that is, the default SQLite DB for the persistent cache.
        /// </summary>
        /// <value>The default cache file.</value>
        public string DefaultCacheFile { get; set; }

        /// <summary>
        ///   Gets or sets the default partition, used when none is specified.
        /// </summary>
        /// <value>The default partition, used when none is specified.</value>
        public string DefaultPartition { get; set; }

        /// <summary>
        ///   Gets or sets the default static interval in days, that is, the default interval for
        ///   "static" items.
        /// </summary>
        /// <value>The default static interval in days.</value>
        public int DefaultStaticIntervalInDays { get; set; }

        /// <summary>
        ///   Gets or sets the default insertion count before automatic clean.
        /// </summary>
        /// <value>The default insertion count before automatic clean.</value>
        public int DefaultInsertionCountBeforeAutoClean { get; set; }

        /// <summary>
        ///   Gets or sets the default maximum cache size in MB.
        /// </summary>
        /// <value>The default maximum cache size in MB.</value>
        public int DefaultMaxCacheSizeInMB { get; set; }

        /// <summary>
        ///   Gets or sets the default maximum journal size in MB.
        /// </summary>
        /// <value>The default maximum journal size in MB.</value>
        public int DefaultMaxJournalSizeInMB { get; set; }
    }
}
