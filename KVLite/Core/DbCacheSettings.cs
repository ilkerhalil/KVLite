﻿// File name: AbstractSQLiteCacheSettings.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.CodeServices.Caching;
using PommaLabs.Thrower;
using System;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    [Serializable, DataContract]
    public class DbCacheSettings<TSettings> : AbstractCacheSettings<TSettings>
        where TSettings : DbCacheSettings<TSettings>
    {
        #region Fields

        private double _chancesOfAutoCleanup;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values for SQL cache settings.
        /// </summary>
        public DbCacheSettings()
        {
            // Default values.
            DefaultPartition = "kvl.default";
            StaticIntervalInDays = 30;
            ChancesOfAutoCleanup = 0.01;
        }

        internal IDbCacheConnectionFactory ConnectionFactory { get; set; }

        #endregion Construction

        #region Settings

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        [IgnoreDataMember]
        public override sealed string CacheUri => ConnectionFactory.ConnectionString;

        /// <summary>
        ///   Chances of an automatic cleanup happening right after an insert operation. Defaults to 1%.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is less than zero or greater than one.
        /// </exception>
        /// <remarks>
        ///   Set this property to zero if you want automatic cleanups to never
        ///   happen. Instead, if you want automatic cleanups to happen on every insert operation,
        ///   you should set this value to one.
        /// </remarks>
        [DataMember]
        public double ChancesOfAutoCleanup
        {
            get
            {
                var result = _chancesOfAutoCleanup;

                // Postconditions
                Debug.Assert(result >= 0.0 && result <= 1.0);
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentOutOfRangeException.IfIsLess(value, 0.0, nameof(value));
                Raise.ArgumentOutOfRangeException.IfIsGreater(value, 1.0, nameof(value));

                _chancesOfAutoCleanup = value;
                OnPropertyChanged();
            }
        }

        #endregion Settings
    }
}