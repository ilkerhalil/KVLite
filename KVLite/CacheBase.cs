//
// CacheBase.cs
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
using System.Threading.Tasks;
using KVLite.Core;

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    /// <typeparam name="TCache"></typeparam>
    [Serializable]
    public abstract class CacheBase<TCache> : ICache<TCache> where TCache : CacheBase<TCache>, ICache<TCache>, new()
    {
        protected const string DefaultPartition = "_DEFAULT_PARTITION_";

        private static readonly TCache CachedDefaultInstance = new TCache();

        internal readonly BinarySerializer BinarySerializer = new BinarySerializer();

        #region Public Properties

        public static TCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        #endregion

        #region ICache Members

        public object this[string partition, string key]
        {
            get { return Get(partition, key); }
        }

        public object this[string key]
        {
            get { return Get(key); }
        }

        public abstract void AddSliding(string partition, string key, object value, TimeSpan interval);

        public void AddSliding(string key, object value, TimeSpan interval)
        {
            AddSliding(DefaultPartition, key, value, interval);
        }

        public abstract void AddStatic(string partition, string key, object value);

        public void AddStatic(string key, object value)
        {
            AddStatic(DefaultPartition, key, value);
        }

        public Task AddStaticAsync(string partition, string key, object value)
        {
            return Task.Factory.StartNew(() => AddStatic(partition, key, value));
        }

        public Task AddStaticAsync(string key, object value)
        {
            return Task.Factory.StartNew(() => AddStatic(key, value));
        }

        public abstract void AddTimed(string partition, string key, object value, DateTime utcExpiry);

        public void AddTimed(string key, object value, DateTime utcExpiry)
        {
            AddTimed(DefaultPartition, key, value, utcExpiry);
        }

        public void Clear()
        {
            Clear(CacheReadMode.ConsiderExpirationDate);
        }

        public abstract void Clear(CacheReadMode cacheReadMode);

        public abstract bool Contains(string partition, string key);

        public bool Contains(string key)
        {
            return Contains(DefaultPartition, key);
        }

        public int Count()
        {
            return (int) LongCount(CacheReadMode.ConsiderExpirationDate);
        }

        public int Count(CacheReadMode cacheReadMode)
        {
            return (int) LongCount(cacheReadMode);
        }

        public long LongCount()
        {
            return LongCount(CacheReadMode.ConsiderExpirationDate);
        }

        public abstract long LongCount(CacheReadMode cacheReadMode);

        public object Get(string partition, string key)
        {
            var item = GetItem(partition, key);
            return item == null ? null : item.Value;
        }

        public object Get(string key)
        {
            return Get(DefaultPartition, key);
        }

        public Task<object> GetAsync(string partition, string key)
        {
            return Task.Factory.StartNew(() => Get(partition, key));
        }

        public Task<object> GetAsync(string key)
        {
            return Task.Factory.StartNew(() => Get(key));
        }

        public abstract CacheItem GetItem(string partition, string key);

        public CacheItem GetItem(string key)
        {
            return GetItem(DefaultPartition, key);
        }

        public Task<CacheItem> GetItemAsync(string partition, string key)
        {
            return Task.Factory.StartNew(() => GetItem(partition, key));
        }

        public Task<CacheItem> GetItemAsync(string key)
        {
            return Task.Factory.StartNew(() => GetItem(key));
        }

        public IList<object> GetAll()
        {
            return DoGetAllItems().Select(x => x.Value).ToList();
        }

        public Task<IList<object>> GetAllAsync()
        {
            return Task.Factory.StartNew((Func<IList<object>>) GetAll);
        }

        public IList<object> GetPartition(string partition)
        {
            return DoGetPartitionItems(partition).Select(x => x.Value).ToList();
        }

        public Task<IList<object>> GetPartitionAsync(string partition)
        {
            return Task.Factory.StartNew(() => GetPartition(partition));
        }

        public IList<CacheItem> GetAllItems()
        {
            return DoGetAllItems().ToList();
        }

        public Task<IList<CacheItem>> GetAllItemsAsync()
        {
            return Task.Factory.StartNew((Func<IList<CacheItem>>) GetAllItems);
        }

        public IList<CacheItem> GetPartitionItems(string partition)
        {
            return DoGetPartitionItems(partition).ToList();
        }

        public Task<IList<CacheItem>> GetPartitionItemsAsync(string partition)
        {
            return Task.Factory.StartNew(() => GetPartitionItems(partition));
        }

        public abstract void Remove(string partition, string key);

        public void Remove(string key)
        {
            Remove(DefaultPartition, key);
        }

        public Task RemoveAsync(string partition, string key)
        {
            return Task.Factory.StartNew(() => Remove(partition, key));
        }

        public Task RemoveAsync(string key)
        {
            return Task.Factory.StartNew(() => Remove(key));
        }

        #endregion

        #region Protected Members

        protected abstract IEnumerable<CacheItem> DoGetAllItems();

        protected abstract IEnumerable<CacheItem> DoGetPartitionItems(string partition);

        #endregion
    }
}
