// File name: OracleCache.cs
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

using Dapper;
using NodaTime;
using Oracle.ManagedDataAccess.Client;
using PommaLabs.KVLite.Database;
using PommaLabs.KVLite.Extensibility;

namespace PommaLabs.KVLite.Oracle
{
    /// <summary>
    ///   Cache backed by Oracle.
    /// </summary>
    public class OracleCache : DbCache<OracleCacheSettings, OracleConnection>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default cache settings.
        /// </summary>
#pragma warning disable CC0022 // Should dispose object

        public static OracleCache DefaultInstance { get; } = new OracleCache(new OracleCacheSettings());

#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        /// <summary>
        ///   Initializes a new instance of the <see cref="OracleCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="random">The random number generator.</param>
        public OracleCache(OracleCacheSettings settings, ISerializer serializer = null, ICompressor compressor = null, IClock clock = null, IRandom random = null)
            : this(settings, new OracleCacheConnectionFactory(), serializer, compressor, clock, random)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="OracleCache"/> class with given settings
        ///   and specified connection factory.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="connectionFactory">Cache connection factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="random">The random number generator.</param>
        public OracleCache(OracleCacheSettings settings, OracleCacheConnectionFactory connectionFactory, ISerializer serializer = null, ICompressor compressor = null, IClock clock = null, IRandom random = null)
            : base(settings, connectionFactory, serializer, compressor, clock, random)
        {
        }

        /// <summary>
        ///   Converts given cache entry into dynamic parameters.
        /// </summary>
        /// <param name="dbCacheEntry">Cache entry.</param>
        /// <returns>Given cache entry converted into dynamic parameters.</returns>
        protected override SqlMapper.IDynamicParameters ToDynamicParameters(DbCacheEntry dbCacheEntry)
        {
            var p = new OracleDynamicParameters(dbCacheEntry);
            p.Add(nameof(DbCacheValue.Value), dbCacheEntry.Value, OracleDbType.Blob);
            return p;
        }
    }
}
