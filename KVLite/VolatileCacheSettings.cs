// File name: VolatileCacheSettings.cs
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
using System;
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Properties;

namespace PommaLabs.KVLite
{
    public sealed class VolatileCacheSettings : CacheSettingsBase
    {
        #region Fields

        private string _cacheFile = Settings.Default.VolatileCache_DefaultCacheName;
        private int _maxCacheSizeInMB = Settings.Default.VolatileCache_DefaultMaxCacheSizeInMB;
        private int _maxJournalSizeInMB = Settings.Default.VolatileCache_DefaultMaxJournalSizeInMB;
        private int _insertionCountBeforeCleanup = Settings.Default.VolatileCache_DefaultInsertionCountBeforeAutoClean;

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

        public override int InsertionCountBeforeAutoClean
        {
            get
            {
                return _insertionCountBeforeCleanup;
            }
            set
            {
                _insertionCountBeforeCleanup = value;
                OnPropertyChanged();
            }
        }

        public override int MaxCacheSizeInMB
        {
            get
            {
                return _maxCacheSizeInMB;
            }
            set
            {
                _maxCacheSizeInMB = value;
                OnPropertyChanged();
            }
        }

        public override int MaxJournalSizeInMB
        {
            get
            {
                return _maxJournalSizeInMB;
            }
            set
            {
                _maxJournalSizeInMB = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings
    }
}