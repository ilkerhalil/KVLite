// File name: AbstractCacheSettings.cs
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

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using PommaLabs.Thrower;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    [Serializable]
    public abstract class AbstractCacheSettings : INotifyPropertyChanged
    {
        #region Fields

        string _defaultPartition;
        int _insertionCountBeforeCleanup;
        int _maxCacheSizeInMB;
        int _maxJournalSizeInMB;
        int _staticIntervalInDays;

        internal TimeSpan StaticInterval;

        #endregion Fields

        #region Settings

        /// <summary>
        ///   The partition used when none is specified.
        /// </summary>
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
                RaiseArgumentException.IfStringIsNullOrWhiteSpace(value, nameof(DefaultPartition));

                _defaultPartition = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   How many days static values will last.
        /// </summary>
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
                RaiseArgumentOutOfRangeException.If(value <= 0);

                _staticIntervalInDays = value;
                StaticInterval = TimeSpan.FromDays(value);
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Number of inserts before a cache cleanup is issued.
        /// </summary>
        public int InsertionCountBeforeAutoClean
        {
            get
            {
                var result = _insertionCountBeforeCleanup;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                RaiseArgumentOutOfRangeException.If(value <= 0);

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
                var result = _maxCacheSizeInMB;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                RaiseArgumentOutOfRangeException.If(value <= 0);

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
                var result = _maxJournalSizeInMB;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                RaiseArgumentOutOfRangeException.If(value <= 0);

                _maxJournalSizeInMB = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings

        #region Abstract Settings

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
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
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        #endregion INotifyPropertyChanged Members
    }
}
