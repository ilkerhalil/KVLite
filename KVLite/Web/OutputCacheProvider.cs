// File name: OutputCacheProvider.cs
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

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   A custom output cache provider based on KVLite.
    /// </summary>
    public sealed class OutputCacheProvider : System.Web.Caching.OutputCacheProvider
    {
        #region Fields

        /// <summary>
        ///   The partition used by output cache provider items.
        /// </summary>
        private const string OutputCachePartition = "KVLite.Web.OutputCache";

        private static ICache _cache = PersistentCache.DefaultInstance;

        #endregion Fields

        #region Properties

        /// <summary>
        ///   Gets or sets the cache instance currently used by the provider.
        /// </summary>
        /// <value>
        ///   The cache instance currently used by the provider.
        /// </value>
        public static ICache Cache
        {
            get
            {
                Contract.Ensures(Contract.Result<ICache>() != null);
                return _cache;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullCache);
                Contract.Ensures(ReferenceEquals(Cache, value));
                _cache = value;
            }
        }

        #endregion

        public override object Get(string key)
        {
            return _cache.Get<object>(OutputCachePartition, key);
        }

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            _cache.AddTimed(OutputCachePartition, key, entry, utcExpiry);
            return entry;
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            _cache.AddTimed(OutputCachePartition, key, entry, utcExpiry);
        }

        public override void Remove(string key)
        {
            _cache.Remove(OutputCachePartition, key);
        }
    }
}