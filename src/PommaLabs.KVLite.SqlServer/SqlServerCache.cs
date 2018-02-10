﻿// File name: SqlServerCache.cs
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

using PommaLabs.KVLite.Database;
using PommaLabs.KVLite.Extensibility;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace PommaLabs.KVLite.SqlServer
{
    /// <summary>
    ///   Cache backed by SQL Server.
    /// </summary>
    public sealed class SqlServerCache : DbCache<SqlServerCache, SqlServerCacheSettings, SqlServerCacheConnectionFactory, SqlConnection>
    {
        #region Default Instance

        /// <summary>
        ///   Gets the default instance for this cache kind. Default instance is configured using
        ///   default cache settings.
        /// </summary>
#pragma warning disable CC0022 // Should dispose object

        public static SqlServerCache DefaultInstance { get; } = new SqlServerCache(new SqlServerCacheSettings());

#pragma warning restore CC0022 // Should dispose object

        #endregion Default Instance

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerCache"/> class with given settings.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="random">The random number generator.</param>
        public SqlServerCache(SqlServerCacheSettings settings, ISerializer serializer = null, ICompressor compressor = null, IClock clock = null, IRandom random = null)
            : this(settings, new SqlServerCacheConnectionFactory(settings), serializer, compressor, clock, random)
        {
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SqlServerCache"/> class with given
        ///   settings and specified connection factory.
        /// </summary>
        /// <param name="settings">Cache settings.</param>
        /// <param name="connectionFactory">Cache connection factory.</param>
        /// <param name="serializer">The serializer.</param>
        /// <param name="compressor">The compressor.</param>
        /// <param name="clock">The clock.</param>
        /// <param name="random">The random number generator.</param>
        public SqlServerCache(SqlServerCacheSettings settings, SqlServerCacheConnectionFactory connectionFactory, ISerializer serializer = null, ICompressor compressor = null, IClock clock = null, IRandom random = null)
            : base(settings, connectionFactory, serializer, compressor, clock, random)
        {
        }

        #region Helpers

        /// <summary>
        ///   A list of error numbers which should not be logged because they are handled by the
        ///   retry logic and they do not provide useful information.
        /// </summary>
        private static readonly HashSet<int> UnloggableErrorNumbers = new HashSet<int>
        {
            1205 // Deadlock
        };

        /// <summary>
        ///   Determines whether given exception should be logged.
        /// </summary>
        /// <param name="exception">The exception.</param>
        /// <returns>True if given exception should be logged, false otherwise.</returns>
        protected override bool ShouldLogException(Exception exception)
        {
            if (exception is SqlException sqlException)
            {
                return !UnloggableErrorNumbers.Contains(sqlException.Number);
            }
            return true;
        }

        #endregion Helpers
    }
}
