// File name: MemoryCacheSettings.cs
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
using System.Text.RegularExpressions;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Settings used by <see cref="MemoryCache"/>.
    /// </summary>
    [Serializable]
    public sealed class MemoryCacheSettings : AbstractCacheSettings
    {
        #region Fields

        string _cacheName = MemoryCacheConfiguration.Instance.DefaultCacheName;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values read from <see cref="MemoryCacheConfiguration"/>.
        /// </summary>
        public MemoryCacheSettings()
        {
            DefaultPartition = MemoryCacheConfiguration.Instance.DefaultPartition;
            StaticIntervalInDays = MemoryCacheConfiguration.Instance.DefaultStaticIntervalInDays;
            MaxCacheSizeInMB = MemoryCacheConfiguration.Instance.DefaultMaxCacheSizeInMB;
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///   Gets the default settings for <see cref="MemoryCache"/>.
        /// </summary>
        /// <value>The default settings for <see cref="MemoryCache"/>.</value>
        [Pure]
        public static MemoryCacheSettings Default { get; } = new MemoryCacheSettings();

        #endregion Properties

        #region Settings

        /// <summary>
        ///   The name of the in-memory store used as the backend for the cache.
        /// </summary>
        public string CacheName
        {
            get
            {
                var result = _cacheName;

                // Postconditions
                Debug.Assert(!string.IsNullOrWhiteSpace(result));
                return result;
            }
            set
            {
                // Preconditions
                RaiseArgumentException.IfStringIsNullOrWhiteSpace(value, nameof(CacheName), ErrorMessages.NullOrEmptyCacheName);
                RaiseArgumentException.IfNot(Regex.IsMatch(value, @"^[a-zA-Z0-9_\-\. ]*$"), ErrorMessages.InvalidCacheName, nameof(CacheName));

                _cacheName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        public override string CacheUri => CacheName;

        #endregion Settings
    }
}
