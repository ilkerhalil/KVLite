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

using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Properties;
using System;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    public sealed class PersistentCacheSettings : CacheSettingsBase
    {
        #region Fields

        private string _cacheFile = Settings.Default.PCache_DefaultFile;
        private int _maxCacheSizeInMB = Settings.Default.PCache_DefaultMaxCacheSizeInMB;
        private int _maxJournalSizeInMB = Settings.Default.PCache_DefaultMaxLogSizeInMB;
        private int _insertionCountBeforeCleanup = Settings.Default.PCache_DefaultInsertionCountBeforeCleanup;

        #endregion Fields

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

        /// <summary>
        ///   Number of inserts before a cache cleanup is issued.
        /// </summary>
        public int InsertionCountBeforeCleanup
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _insertionCountBeforeCleanup;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _insertionCountBeforeCleanup = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Max size in megabytes for the cache.
        /// </summary>
        public int MaxCacheSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _maxCacheSizeInMB;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _maxCacheSizeInMB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Max size in megabytes for the SQLite journal log.
        /// </summary>
        public int MaxJournalSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _maxJournalSizeInMB;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _maxJournalSizeInMB = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings
    }
}