// VolatileCache.cs
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

using System.Diagnostics;
using Newtonsoft.Json;
using PommaLabs.GRAMPA;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;

namespace PommaLabs.KVLite
{
    [Serializable]
    public sealed class VolatileCache : CacheBase<VolatileCache, VolatileCacheSettings>
    {
        #region Construction

        public VolatileCache()
            : this(new VolatileCacheSettings())
        {
        }

        public VolatileCache(VolatileCacheSettings settings)
            : base(settings)
        {
        }

        #endregion

        #region ICache Members

        public override CacheKind Kind
        {
            get { return CacheKind.Volatile; }
        }

        public override bool Contains(string partition, string key)
        {
            return Settings.MemoryCache.Contains(CreateKey(partition, key));
        }

        public override void Remove(string partition, string key)
        {
            Settings.MemoryCache.Remove(CreateKey(partition, key));
        }

        #endregion

        #region CacheBase Members

        protected override void DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            var item = new CacheItem
            {
                Partition = partition,
                Key = key,
                UtcCreation = DateTime.UtcNow,
                UtcExpiry = utcExpiry,
                Interval = interval,
                Value = value
            };

            if (interval.HasValue)
            {
                Settings.MemoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy
                {
                    SlidingExpiration = interval.Value
                });
            }
            else
            {
                Debug.Assert(utcExpiry != null);
                Settings.MemoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy
                {
                    AbsoluteExpiration = utcExpiry.Value.ToLocalTime()
                });
            }
        }

        protected override void DoClear(string partition)
        {
            var items = Settings.MemoryCache.AsEnumerable();

            if (partition != null)
            {
                items = items.Where(x =>
                {
                    var val = x.Value as CacheItem;
                    return val != null && val.Partition == partition;
                });
            }

            items = items.ToList();

            foreach (var item in items)
            {
                Settings.MemoryCache.Remove(item.Key);
            }
        }

        protected override long DoCount(string partition)
        {
            return Settings.MemoryCache.Count(x =>
            {
                var val = x.Value as CacheItem;
                return val != null && (partition == null || val.Partition == partition);
            });
        }

        protected override object DoGetOne(string partition, string key)
        {
            var item = DoGetOneItem(partition, key);
            return (item == null) ? null : item.Value;
        }

        protected override CacheItem DoGetOneItem(string partition, string key)
        {
            var item = Settings.MemoryCache.Get(CreateKey(partition, key)) as CacheItem;

            // Expiry date is updated, if sliding.
            if (item != null && item.Interval.HasValue)
            {
                item.UtcExpiry = item.UtcExpiry + item.Interval;
            }

            return item;
        }

        protected override IList<CacheItem> DoGetManyItems(string partition)
        {
            var items = Settings.MemoryCache.AsEnumerable();

            if (partition != null)
            {
                items = items.Where(x =>
                {
                    var val = x.Value as CacheItem;
                    return val != null && val.Partition == partition;
                });
            }

            var ret = items.Select(x => x.Value as CacheItem).ToList();

            // Expiry dates are updated, if sliding.
            foreach (var item in ret.Where(i => i.Interval.HasValue))
            {
                item.UtcExpiry = item.UtcExpiry + item.Interval;
            }

            return ret;
        }

        protected override object DoPeekOne(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.VolatileCache_CannotPeek);
        }

        protected override CacheItem DoPeekOneItem(string partition, string key)
        {
            throw new NotImplementedException(ErrorMessages.VolatileCache_CannotPeek);
        }

        protected override IList<CacheItem> DoPeekManyItems(string partition)
        {
            throw new NotImplementedException(ErrorMessages.VolatileCache_CannotPeek);
        }

        #endregion

        #region Private Methods

        private static string CreateKey(string partition, string key)
        {
            return JsonConvert.SerializeObject(GPair.Create(partition, key), Formatting.None);
        }

        #endregion Private Methods
    }
}