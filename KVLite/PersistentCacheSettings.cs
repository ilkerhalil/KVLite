﻿// File name: PersistentCacheSettings.cs
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

using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Settings used by <see cref="PersistentCache"/>.
    /// </summary>
    [Serializable]
    public sealed class PersistentCacheSettings : AbstractCacheSettings
    {
        #region Fields

        string _cacheFile = "PersistentCache.sqlite";

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values for persistent cache settings.
        /// </summary>
        public PersistentCacheSettings()
        {
            DefaultPartition = "KVLite.DefaultPartition";
            StaticIntervalInDays = 30;
            InsertionCountBeforeAutoClean = 64;
            MaxCacheSizeInMB = 1024;
            MaxJournalSizeInMB = 64;
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///   Gets the default settings for <see cref="PersistentCache"/>.
        /// </summary>
        /// <value>The default settings for <see cref="PersistentCache"/>.</value>
        [Pure]
        public static PersistentCacheSettings Default { get; } = new PersistentCacheSettings();

        #endregion Properties

        #region Settings

        /// <summary>
        ///   The SQLite DB used as the backend for the cache.
        /// 
        ///   Default value is "PersistentCache.sqlite".
        /// </summary>
        public string CacheFile
        {
            get
            {
                var result = _cacheFile;

                // Postconditions
                Debug.Assert(!string.IsNullOrWhiteSpace(result));
                return result;
            }
            set
            {
                // Preconditions
                RaiseArgumentException.IfIsNullOrWhiteSpace(value, nameof(CacheFile), ErrorMessages.NullOrEmptyCacheFile);

                _cacheFile = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        public override string CacheUri => CacheFile;

        #endregion Settings
    }
}
