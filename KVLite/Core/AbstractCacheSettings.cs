// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using PommaLabs.Thrower;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    [Serializable, DataContract]
    public abstract class AbstractCacheSettings<TSettings> : ICacheSettings, IAsyncCacheSettings
        where TSettings : AbstractCacheSettings<TSettings>
    {
        private string _defaultPartition;
        private int _staticIntervalInDays;

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
                Raise.ArgumentException.IfIsNullOrWhiteSpace(value, nameof(DefaultPartition));

                _defaultPartition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   How many days static values will last.
        /// </summary>
        [DataMember]
        public int StaticIntervalInDays
        {
            get
            {
                var result = _staticIntervalInDays;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentOutOfRangeException.If(value <= 0);

                _staticIntervalInDays = value;
                StaticInterval = TimeSpan.FromDays(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   How long static values will last. Computed from <see cref="StaticIntervalInDays"/>.
        /// </summary>
        [IgnoreDataMember]
        public TimeSpan StaticInterval { get; private set; }

        #region Abstract Settings

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        [IgnoreDataMember]
        public abstract string CacheUri { get; }

        #endregion Abstract Settings

        #region INotifyPropertyChanged Members

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

        #endregion INotifyPropertyChanged Members
    }
}
