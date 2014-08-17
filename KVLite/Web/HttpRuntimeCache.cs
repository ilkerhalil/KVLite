//
// HttpRuntimeCache.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Text;
using System.Web;
using System.Web.Caching;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   TODO
    /// </summary>
    public sealed class HttpRuntimeCache : CacheBase<HttpRuntimeCache>
    {
        private static readonly Cache HttpCache = HttpRuntime.Cache ?? new Cache();

        public override void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            throw new NotImplementedException();
        }

        public override void AddStatic(string partition, string key, object value)
        {
            var serializedKey = BinarySerializer.SerializeObject(Tuple.Create(partition, key));
            var serializedValue = BinarySerializer.SerializeObject(value);
            HttpCache.Add(Encoding.Default.GetString(serializedKey), serializedValue, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.Default, null);
        }

        public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            throw new NotImplementedException();
        }

        public override void Clear(CacheReadMode cacheReadMode)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(string partition, string key)
        {
            throw new NotImplementedException();
        }

        public override long LongCount(CacheReadMode cacheReadMode)
        {
            throw new NotImplementedException();
        }

        public override CacheItem GetItem(string partition, string key)
        {
            throw new NotImplementedException();
        }

        public override void Remove(string partition, string key)
        {
            throw new NotImplementedException();
        }

        protected override IList<CacheItem> DoGetAllItems()
        {
            throw new NotImplementedException();
        }

        protected override IList<CacheItem> DoGetPartitionItems(string partition)
        {
            throw new NotImplementedException();
        }
    }
}