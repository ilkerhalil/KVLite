// File name: OracleCacheSettings.cs
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

using Oracle.ManagedDataAccess.Client;
using PommaLabs.KVLite.Core;
using System;
using System.Diagnostics.Contracts;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite.Oracle
{
    /// <summary>
    ///   Settings used by <see cref="OracleCache"/>.
    /// </summary>
    [Serializable, DataContract]
    public class OracleCacheSettings : DbCacheSettings<OracleCacheSettings, OracleConnection>
    {
        #region Properties

        /// <summary>
        ///   Gets the default settings for <see cref="OracleCache"/>.
        /// </summary>
        /// <value>The default settings for <see cref="OracleCache"/>.</value>
        [Pure]
        public static OracleCacheSettings Default { get; } = new OracleCacheSettings();

        #endregion Properties
    }
}
