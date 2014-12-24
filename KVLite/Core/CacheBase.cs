// File name: CacheBase.cs
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
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Base class for caches, implements common functionalities.
    /// </summary>
    /// <typeparam name="TCache">The type of the cache.</typeparam>
    /// <typeparam name="TCacheSettings">The type of the cache settings.</typeparam>
    [Serializable]
    public abstract class CacheBase<TCache, TCacheSettings> : ICache<TCache, TCacheSettings> 
        where TCache : CacheBase<TCache, TCacheSettings>, ICache<TCache, TCacheSettings>, new()
        where TCacheSettings : CacheSettingsBase, new()
    {
        internal const string DefaultPartition = "_DEFAULT_PARTITION_";

        private static readonly TCache CachedDefaultInstance = new TCache();

        private readonly TCacheSettings _settings;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="settings"></param>
        internal CacheBase(TCacheSettings settings)
        {
            Contract.Requires<ArgumentNullException>(settings != null);
            _settings = settings;
        }

        #region Public Properties

        /// <summary>
        ///   TODO
        /// </summary>
        public static TCache DefaultInstance
        {
            get { return CachedDefaultInstance; }
        }

        #endregion

        #region ICache<TCache> Members

        public abstract CacheKind Kind { get; }

        public TCacheSettings Settings
        {
            get { return _settings; }
        }

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

        public Task AddSlidingAsync(string partition, string key, object value, TimeSpan interval)
        {
            return TaskEx.Run(() => AddSliding(partition, key, value, interval));
        }

        public Task AddSlidingAsync(string key, object value, TimeSpan interval)
        {
            return TaskEx.Run(() => AddSliding(DefaultPartition, key, value, interval));
        }

        public void AddStatic(string partition, string key, object value)
        {
            AddSliding(partition, key, value, TimeSpan.FromDays(Settings.StaticIntervalInDays));
        }

        public void AddStatic(string key, object value)
        {
            AddSliding(DefaultPartition, key, value, TimeSpan.FromDays(Settings.StaticIntervalInDays));
        }

        public Task AddStaticAsync(string partition, string key, object value)
        {
            return TaskEx.Run(() => AddSliding(partition, key, value, TimeSpan.FromDays(Settings.StaticIntervalInDays)));
        }

        public Task AddStaticAsync(string key, object value)
        {
            return TaskEx.Run(() => AddSliding(DefaultPartition, key, value, TimeSpan.FromDays(Settings.StaticIntervalInDays)));
        }

        public abstract void AddTimed(string partition, string key, object value, DateTime utcExpiry);

        public void AddTimed(string key, object value, DateTime utcExpiry)
        {
            AddTimed(DefaultPartition, key, value, utcExpiry);
        }

        public Task AddTimedAsync(string partition, string key, object value, DateTime utcExpiry)
        {
            return TaskEx.Run(() => AddTimed(partition, key, value, utcExpiry));
        }

        public Task AddTimedAsync(string key, object value, DateTime utcExpiry)
        {
            return TaskEx.Run(() => AddTimed(DefaultPartition, key, value, utcExpiry));
        }

        public abstract void Clear();

        public abstract void Clear(string partition);

        public abstract bool Contains(string partition, string key);

        public bool Contains(string key)
        {
            return Contains(DefaultPartition, key);
        }

        public int Count()
        {
            return Convert.ToInt32(LongCount());
        }

        public int Count(string partition)
        {
            return Convert.ToInt32(LongCount(partition));
        }

        public abstract long LongCount();

        public abstract long LongCount(string partition);

        public object Get(string partition, string key)
        {
            var item = GetItem(partition, key);
            return item == null ? null : item.Value;
        }

        public object Get(string key)
        {
            var item = GetItem(DefaultPartition, key);
            return item == null ? null : item.Value;
        }

        public abstract CacheItem GetItem(string partition, string key);

        public CacheItem GetItem(string key)
        {
            return GetItem(DefaultPartition, key);
        }

        public IList<object> GetAll()
        {
            return DoGetAllItems().Select(x => x.Value).ToList();
        }

        public IList<object> GetAll(string partition)
        {
            return DoGetPartitionItems(partition).Select(x => x.Value).ToList();
        }

        public IList<CacheItem> GetAllItems()
        {
            return DoGetAllItems().ToList();
        }

        public IList<CacheItem> GetAllItems(string partition)
        {
            return DoGetPartitionItems(partition).ToList();
        }

        public abstract void Remove(string partition, string key);

        public void Remove(string key)
        {
            Remove(DefaultPartition, key);
        }

        public Task RemoveAsync(string partition, string key)
        {
            return TaskEx.Run(() => Remove(partition, key));
        }

        public Task RemoveAsync(string key)
        {
            return TaskEx.Run(() => Remove(DefaultPartition, key));
        }

        #endregion

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        protected abstract IEnumerable<CacheItem> DoGetAllItems();

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <returns></returns>
        protected abstract IEnumerable<CacheItem> DoGetPartitionItems(string partition);
    }
}