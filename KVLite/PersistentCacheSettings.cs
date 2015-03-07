// File name: PersistentCacheSettings.cs
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
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Properties;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Settings used by <see cref="PersistentCache"/>.
    /// </summary>
    [Serializable]
    public sealed class PersistentCacheSettings : CacheSettingsBase
    {
        #region Fields

        private string _cacheFile = Settings.Default.PersistentCache_DefaultCacheFile;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values read from <see cref="Settings"/>.
        /// </summary>
        public PersistentCacheSettings()
        {
            InsertionCountBeforeAutoClean = Settings.Default.PersistentCache_DefaultInsertionCountBeforeAutoClean;
            MaxCacheSizeInMB = Settings.Default.PersistentCache_DefaultMaxCacheSizeInMB;
            MaxJournalSizeInMB = Settings.Default.PersistentCache_DefaultMaxJournalSizeInMB;
        }

        #endregion Construction

        #region Settings

        /// <summary>
        ///   The SQLite DB used as the backend for the cache.
        /// </summary>
        public string CacheFile
        {
            get
            {
                Contract.Ensures(!String.IsNullOrWhiteSpace(Contract.Result<string>()));
                return _cacheFile;
            }
            set
            {
                Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(value), ErrorMessages.NullOrEmptyCachePath);
                _cacheFile = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings
    }
}