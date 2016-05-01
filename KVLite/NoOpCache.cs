// File name: NoOpCache.cs
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

using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   A cache which does really nothing. It might be used for unit testing.
    /// </summary>
    public sealed class NoOpCache : AbstractCache<NoOpCacheSettings>
    {
        public override bool CanPeek
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override IClock Clock
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ICompressor Compressor
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ILog Log
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override int MaxParentKeyCountPerItem
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override ISerializer Serializer
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public override NoOpCacheSettings Settings
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        protected override void AddInternal<TVal>(string partition, string key, TVal value, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            // Nothing to do.
        }

        protected override long ClearInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.IgnoreExpiryDate) => 0L;

        protected override bool ContainsInternal(string partition, string key) => false;

        protected override long CountInternal(string partition, CacheReadMode cacheReadMode = CacheReadMode.ConsiderExpiryDate) => 0L;

        protected override void Dispose(bool disposing)
        {
            // Nothing to do.
        }

        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> GetInternal<TVal>(string partition, string key) => Option.None<TVal>();

        protected override Option<CacheItem<TVal>> GetItemInternal<TVal>(string partition, string key) => Option.None<CacheItem<TVal>>();

        protected override CacheItem<TVal>[] GetItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        protected override Option<TVal> PeekInternal<TVal>(string partition, string key) => Option.None<TVal>();

        protected override Option<CacheItem<TVal>> PeekItemInternal<TVal>(string partition, string key) => Option.None<CacheItem<TVal>>();

        protected override CacheItem<TVal>[] PeekItemsInternal<TVal>(string partition)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        protected override void RemoveInternal(string partition, string key)
        {
            // Nothing to do.
        }
    }
}