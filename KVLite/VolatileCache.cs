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
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.Caching;
using Newtonsoft.Json;
using PommaLabs.GRAMPA;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
   [Serializable]
   public sealed class VolatileCache : CacheBase<VolatileCache>
   {
      private readonly MemoryCache _memoryCache;

      public VolatileCache() : this(MemoryCache.Default)
      {
      }

      public VolatileCache(MemoryCache memoryCache)
      {
         Contract.Requires<ArgumentNullException>(memoryCache != null);
         _memoryCache = memoryCache;
      }

      public override CacheKind Kind
      {
         get { return CacheKind.Volatile; }
      }

      public override void AddSliding(string partition, string key, object value, TimeSpan interval)
      {
         var item = new CacheItem
         {
            Partition = partition,
            Key = key,
            UtcCreation = DateTime.UtcNow,
            UtcExpiry = null,
            Interval = interval,
            Value = value
         };
         _memoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy {SlidingExpiration = interval});
      }

      public override void AddTimed(string partition, string key, object value, DateTime utcExpiry)
      {
         var item = new CacheItem
         {
            Partition = partition,
            Key = key,
            UtcCreation = DateTime.UtcNow,
            UtcExpiry = utcExpiry,
            Interval = null,
            Value = value
         };
         _memoryCache.Set(CreateKey(partition, key), item, new CacheItemPolicy {AbsoluteExpiration = utcExpiry.ToLocalTime()});
      }

      public override void Clear(CacheReadMode cacheReadMode)
      {
         var items = _memoryCache.Where(x => x.Value is CacheItem).Select(x => x.Key).ToList();
         foreach (var item in items)
         {
            _memoryCache.Remove(item);
         }
      }

      public override bool Contains(string partition, string key)
      {
         return _memoryCache.Contains(CreateKey(partition, key));
      }

      public override long LongCount(CacheReadMode cacheReadMode)
      {
         return _memoryCache.GetCount();
      }

      public override CacheItem GetItem(string partition, string key)
      {
         return _memoryCache.Get(CreateKey(partition, key)) as CacheItem;
      }

      public override void Remove(string partition, string key)
      {
         _memoryCache.Remove(CreateKey(partition, key));
      }

      protected override IList<CacheItem> DoGetAllItems()
      {
         return _memoryCache.Where(x => x.Value is CacheItem).Select(x => x.Value as CacheItem).ToList();
      }

      protected override IList<CacheItem> DoGetPartitionItems(string partition)
      {
         return _memoryCache.Where(x =>
         {
            var val = x.Value as CacheItem;
            return val != null && val.Partition == partition;
         }).Select(x => x.Value as CacheItem).ToList();
      }

      private static string CreateKey(string partition, string key)
      {
         return JsonConvert.SerializeObject(GPair.Create(partition, key));
      }
   }
}