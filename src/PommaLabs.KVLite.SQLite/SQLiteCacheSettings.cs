// File name: SQLiteCacheSettings.cs
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

using PommaLabs.KVLite.Database;
using PommaLabs.Thrower;
using System;
using System.Data.SQLite;
using System.Diagnostics;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite.SQLite
{
    /// <summary>
    ///   Settings used by SQLite caches.
    /// </summary>
    [Serializable, DataContract]
    public abstract class SQLiteCacheSettings<TSettings> : DbCacheSettings<TSettings, SQLiteConnection>
        where TSettings : SQLiteCacheSettings<TSettings>
    {
        /// <summary>
        ///   Backing field for <see cref="MaxCacheSizeInMB"/>.
        /// </summary>
        private int _maxCacheSizeInMB;

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
                Raise.ArgumentOutOfRangeException.If(value <= 0);

                _maxCacheSizeInMB = value;
                OnPropertyChanged();
            }
        }
    }
}
