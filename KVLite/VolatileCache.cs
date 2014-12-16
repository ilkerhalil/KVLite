//
// VolatileCache.cs
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
using System.Linq;
using System.Runtime.Caching;
using Newtonsoft.Json;
using PommaLabs.GRAMPA;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    [Serializable]
    public sealed class VolatileCache : CacheBase<VolatileCache, VolatileCacheSettings>
    {
        public VolatileCache() : this(new VolatileCacheSettings())
        {
        }

        public VolatileCache(VolatileCacheSettings settings) : base(settings)
        {
        }

        public override CacheKind Kind
        {
            get { return CacheKind.Volatile; }
        }

        public override void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            var item = new CacheItem {
                Partition = partition,
                Key = key,
                UtcCreation = DateTime.UtcNow,
                UtcExpiry = DateTime.UtcNow + interval,
                Interval = interval,
                Value = value
            };
            Settings.MemoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy {SlidingExpiration = interval});
        }

        public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            var item = new CacheItem {
                Partition = partition,
                Key = key,
                UtcCreation = DateTime.UtcNow,
                UtcExpiry = utcExpiry,
                Interval = null,
                Value = value
            };
            Settings.MemoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy {AbsoluteExpiration = utcExpiry.ToLocalTime()});
        }

        public override void Clear()
        {
            var items = Settings.MemoryCache.Where(x => x.Value is CacheItem).Select(x => x.Key).ToList();
            foreach (var item in items) {
                Settings.MemoryCache.Remove(item);
            }
        }

        public override bool Contains(string partition, string key)
        {
            return GetItem(partition, key) != null;
        }

        public override long LongCount()
        {
            return Settings.MemoryCache.GetCount();
        }

        public override CacheItem GetItem(string partition, string key)
        {
            var item = Settings.MemoryCache.Get(CreateKey(partition, key)) as CacheItem;
            
            // Expiry date is updated, if sliding.
            if (item != null && item.Interval.HasValue) {
                item.UtcExpiry = item.UtcExpiry + item.Interval;
            }

            return item;
        }

        public override void Remove(string partition, string key)
        {
            Settings.MemoryCache.Remove(CreateKey(partition, key));
        }

        protected override IList<CacheItem> DoGetAllItems()
        {
            var items = Settings.MemoryCache.Where(x => x.Value is CacheItem).Select(x => x.Value as CacheItem).ToList();

            // Expiry dates are updated, if sliding.
            foreach (var item in items.Where(i => i.Interval.HasValue)) {
                item.UtcExpiry = item.UtcExpiry + item.Interval;
            }

            return items;
        }

        protected override IList<CacheItem> DoGetPartitionItems(string partition)
        {
            var items = Settings.MemoryCache.Where(x => {
                var val = x.Value as CacheItem;
                return val != null && val.Partition == partition;
            }).Select(x => x.Value as CacheItem).ToList();

            // Expiry dates are updated, if sliding.
            foreach (var item in items.Where(i => i.Interval.HasValue)) {
                item.UtcExpiry = item.UtcExpiry + item.Interval;
            }

            return items;
        }

        private static string CreateKey(string partition, string key)
        {
            return JsonConvert.SerializeObject(GPair.Create(partition, key));
        }
    }
}