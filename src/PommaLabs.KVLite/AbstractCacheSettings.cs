// File name: AbstractCacheSettings.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
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

using PommaLabs.KVLite.Logging;
using PommaLabs.KVLite.Resources;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    [Serializable, DataContract]
    public abstract partial class AbstractCacheSettings<TSettings> : ICacheSettings
        where TSettings : AbstractCacheSettings<TSettings>
    {
        /// <summary>
        ///   Log for cache settings class.
        /// </summary>
        protected static ILog Log { get; } = LogProvider.For<TSettings>();

        /// <summary>
        ///   Backing field for <see cref="CacheName"/>.
        /// </summary>
        private string _cacheName = typeof(TSettings).Name.Replace("Settings", string.Empty);

        /// <summary>
        ///   The cache name, can be used for logging.
        /// </summary>
        /// <value>The cache name.</value>
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

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(CacheName), _cacheName, value);
                _cacheName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="DefaultDistributedCacheAbsoluteExpiration"/>.
        /// </summary>
        private TimeSpan _defaultDistributedCacheAbsoluteExpiration = TimeSpan.FromMinutes(20);

        /// <summary>
        ///   Default absolute expiration for distributed cache entries. Initial value is 20 minutes.
        /// </summary>
        public TimeSpan DefaultDistributedCacheAbsoluteExpiration
        {
            get
            {
                var result = _defaultDistributedCacheAbsoluteExpiration;

                // Postconditions
                Debug.Assert(result.Ticks > 0L);
                return result;
            }
            set
            {
                // Preconditions
                if (value.Ticks <= 0L) throw new ArgumentOutOfRangeException(nameof(DefaultDistributedCacheAbsoluteExpiration));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(DefaultDistributedCacheAbsoluteExpiration), _defaultDistributedCacheAbsoluteExpiration, value);
                _defaultDistributedCacheAbsoluteExpiration = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="DefaultPartition"/>.
        /// </summary>
        private string _defaultPartition = CachePartitions.Default;

        /// <summary>
        ///   The partition used when none is specified.
        /// </summary>
        [DataMember]
        public string DefaultPartition
        {
            get
            {
                var result = _defaultPartition;

                // Postconditions
                Debug.Assert(!string.IsNullOrWhiteSpace(result));
                return result;
            }
            set
            {
                // Preconditions
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(ErrorMessages.NullOrEmptyDefaultPartition, nameof(DefaultPartition));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(DefaultPartition), _defaultPartition, value);
                _defaultPartition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="MinValueLengthForCompression"/>.
        /// </summary>
        private long _minValueLengthForCompression = 4096;

        /// <summary>
        ///   When a serialized value is longer than specified length, then the cache will compress
        ///   it. If a serialized value length is less than or equal to the specified length, then
        ///   the cache will not compress it. Defaults to 4096 bytes.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is less than zero.
        /// </exception>
        [DataMember]
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

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(MinValueLengthForCompression), _minValueLengthForCompression, value);
                _minValueLengthForCompression = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="StaticInterval"/>.
        /// </summary>
        private TimeSpan _staticInterval = TimeSpan.FromDays(30);

        /// <summary>
        ///   How long static values will last.
        /// </summary>
        [DataMember]
        public TimeSpan StaticInterval
        {
            get
            {
                var result = _staticInterval;

                // Postconditions
                Debug.Assert(result.Ticks > 0L);
                return result;
            }
            set
            {
                // Preconditions
                if (value.Ticks <= 0L) throw new ArgumentOutOfRangeException(nameof(StaticInterval));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(StaticInterval), _staticInterval, value);
                _staticInterval = value;
                OnPropertyChanged();
            }
        }

        #region INotifyPropertyChanged

        /// <summary>
        ///   Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        ///   Called when a property changed.
        /// </summary>
        /// <param name="propertyName">Name of the property.</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion INotifyPropertyChanged
    }
}
