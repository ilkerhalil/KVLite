// File name: CacheSettingsContract.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite.Contracts
{
    [ContractClassFor(typeof(CacheSettingsBase))]
    internal abstract class CacheSettingsContract : CacheSettingsBase
    {
        /// <summary>
        ///   Number of inserts before a cache cleanup is issued.
        /// </summary>
        public override int InsertionCountBeforeAutoClean
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return default(int);
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
            }
        }

        /// <summary>
        ///   Max size in megabytes for the cache.
        /// </summary>
        public override int MaxCacheSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return default(int);
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
            }
        }

        /// <summary>
        ///   Max size in megabytes for the SQLite journal log.
        /// </summary>
        public override int MaxJournalSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return default(int);
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
            }
        }
    }
}
