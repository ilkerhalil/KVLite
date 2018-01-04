// File name: DbCacheSettings.cs
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
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace PommaLabs.KVLite.Database
{
    /// <summary>
    ///   Base class for cache settings. Contains settings shared among different caches.
    /// </summary>
    /// <typeparam name="TSettings">The type of the cache settings.</typeparam>
    [Serializable, DataContract]
    public class DbCacheSettings<TSettings> : AbstractCacheSettings<TSettings>
        where TSettings : DbCacheSettings<TSettings>
    {
        /// <summary>
        ///   Used to validate SQL names.
        /// </summary>
        private static Regex IsValidSqlNameRegex { get; } = new Regex("[a-z0-9_]*", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        ///   Backing field for <see cref="CacheSchemaName"/>.
        /// </summary>
        private string _cacheSchemaName = string.Empty;

        /// <summary>
        ///   The schema which holds cache entries table.
        /// </summary>
        [DataMember]
        public string CacheSchemaName
        {
            get
            {
                var result = _cacheSchemaName;

                // Postconditions
                Debug.Assert(IsValidSqlNameRegex.IsMatch(result));
                return result;
            }
            set
            {
                // Preconditions
                if (!IsValidSqlNameRegex.IsMatch(value)) throw new ArgumentException(ErrorMessages.InvalidCacheSchemaName, nameof(CacheSchemaName));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(CacheSchemaName), _cacheSchemaName, value);
                _cacheSchemaName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="CacheEntriesTableName"/>.
        /// </summary>
        private string _cacheEntriesTableName = "kvl_cache_entries";

        /// <summary>
        ///   The name of the table which holds cache entries.
        /// </summary>
        [DataMember]
        public string CacheEntriesTableName
        {
            get
            {
                var result = _cacheEntriesTableName;

                // Postconditions
                Debug.Assert(IsValidSqlNameRegex.IsMatch(result));
                return result;
            }
            set
            {
                // Preconditions
                if (!IsValidSqlNameRegex.IsMatch(value)) throw new ArgumentException(ErrorMessages.InvalidCacheEntriesTableName, nameof(CacheEntriesTableName));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(CacheEntriesTableName), _cacheEntriesTableName, value);
                _cacheEntriesTableName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="ChancesOfAutoCleanup"/>.
        /// </summary>
        private double _chancesOfAutoCleanup = 0.01;

        /// <summary>
        ///   Chances of an automatic cleanup happening right after an insert operation. Defaults to 1%.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="value"/> is less than zero or greater than one.
        /// </exception>
        /// <remarks>
        ///   Set this property to zero if you want automatic cleanups to never happen. Instead, if
        ///   you want automatic cleanups to happen on every insert operation, you should set this
        ///   value to one.
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
                if (value < 0.0 || value > 1.0) throw new ArgumentOutOfRangeException(nameof(ChancesOfAutoCleanup));

                Log.DebugFormat(DebugMessages.UpdateSetting, nameof(ChancesOfAutoCleanup), _chancesOfAutoCleanup, value);
                _chancesOfAutoCleanup = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Backing field for <see cref="ConnectionString"/>.
        /// </summary>
        private string _connectionString;

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        /// <exception cref="ArgumentException">
        ///   <paramref name="value"/> is null, empty or blank.
        /// </exception>
        [DataMember]
        public string ConnectionString
        {
            get
            {
                var result = _connectionString;

                // Postconditions
                Debug.Assert(!string.IsNullOrWhiteSpace(result));
                return result;
            }
            set
            {
                // Preconditions
                if (string.IsNullOrWhiteSpace(value)) throw new ArgumentException(ErrorMessages.NullOrEmptyConnectionString, nameof(ConnectionString));

                // Avoid logging connection string, in order not to expose sensitive information.
                //Log.DebugFormat(DebugMessages.UpdateSetting, nameof(ConnectionString), _connectionString, value);
                _connectionString = value;
                OnPropertyChanged();
            }
        }
    }
}
