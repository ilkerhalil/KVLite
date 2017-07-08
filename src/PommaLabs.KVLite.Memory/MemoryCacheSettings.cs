// File name: MemoryCacheSettings.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using PommaLabs.KVLite.Resources;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PommaLabs.KVLite.Memory
{
    /// <summary>
    ///   Settings used by <see cref="MemoryCache"/>.
    /// </summary>
    [Serializable, DataContract]
    public sealed class MemoryCacheSettings : AbstractCacheSettings<MemoryCacheSettings>
    {
        #region Fields

        private string _cacheName = nameof(MemoryCache);
        private int _maxCacheSizeInMB;
        private long _minValueLengthForCompression;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values for memory cache settings.
        /// </summary>
        public MemoryCacheSettings()
        {
            DefaultPartition = CachePartitions.Default;
            StaticIntervalInDays = 30;
            MaxCacheSizeInMB = 256;
            MinValueLengthForCompression = 4096;
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///   Gets the default settings for <see cref="MemoryCache"/>.
        /// </summary>
        /// <value>The default settings for <see cref="MemoryCache"/>.</value>
        public static MemoryCacheSettings Default { get; } = new MemoryCacheSettings();

        #endregion Properties

        #region Settings

        /// <summary>
        ///   Max size in megabytes for the cache.
        /// </summary>
        [DataMember]
        public int MaxCacheSizeInMB
        {
            get
            {
                var result = _maxCacheSizeInMB;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                if (value <= 0) throw new ArgumentOutOfRangeException(nameof(MaxCacheSizeInMB));

                _maxCacheSizeInMB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   The name of the in-memory store used as the backend for the cache.
        ///
        ///   Default value is "MemoryCache".
        /// </summary>
        [DataMember]
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
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(ErrorMessages.NullOrEmptyCacheName, nameof(CacheName));
                if (!Regex.IsMatch(value, @"^[a-zA-Z0-9_\-\. ]*$")) throw new ArgumentException(ErrorMessages.InvalidCacheName, nameof(CacheName));

                _cacheName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        [IgnoreDataMember]
        public override string CacheUri => CacheName;

        /// <summary>
        ///   When a serialized value is longer than specified quantity, then the cache will compress
        ///   it. If a serialized value length is less than or equal to the specified quantity, then
        ///   the cache will not compress it. Defaults to 4096 bytes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is less than zero.
        /// </exception>
        public long MinValueLengthForCompression
        {
            get
            {
                var result = _minValueLengthForCompression;

                // Postconditions
                Debug.Assert(result >= 0L);
                return result;
            }
            set
            {
                // Preconditions
                if (value < 0L) throw new ArgumentOutOfRangeException(nameof(MinValueLengthForCompression));

                _minValueLengthForCompression = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings
    }
}
