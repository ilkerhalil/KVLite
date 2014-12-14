//
// ICache.cs
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
using System.Threading.Tasks;
using Newtonsoft.Json;
using PommaLabs.KVLite.Contracts;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Represents all kind of caches currently implemented inside KVLite.
    /// </summary>
    public enum CacheKind : byte
    {
        /// <summary>
        ///   The persistent cache, available through the <see cref="PersistentCache"/> class.
        ///   A persistent cache stores its data inside an SQLite database.
        /// </summary>
        Persistent = 1,

        /// <summary>
        ///   The volatile cache, available through the <see cref="VolatileCache"/> class.
        ///   A volatile cache stores its data inside an instance of <see cref="System.Runtime.Caching.MemoryCache"/>.
        /// </summary>
        Volatile = 2
    }

    /// <summary>
    ///   Represents a partition based key-value store. Each (partition, key, value) triple
    ///   has attached either an expiry time or a refresh interval, because values should not
    ///   be stored forever inside a cache.<br/>
    ///   In fact, a cache is, almost by definition, a transient store, used to temporaly
    ///   store the results of time consuming operations.
    /// </summary>
    [ContractClass(typeof(CacheContract))]
    public interface ICache
    {
        /// <summary>
        ///   The kind of cache implementation underlying this interface.
        /// </summary>
        CacheKind Kind { get; }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        object this[string partition, string key] { get; }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        object this[string key] { get; }

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        void AddSliding(string partition, string key, object value, TimeSpan interval);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        void AddSliding(string key, object value, TimeSpan interval);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        Task AddSlidingAsync(string partition, string key, object value, TimeSpan interval);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="interval"></param>
        /// <returns></returns>
        Task AddSlidingAsync(string key, object value, TimeSpan interval);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        void AddStatic(string partition, string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        void AddStatic(string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task AddStaticAsync(string partition, string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task AddStaticAsync(string key, object value);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        void AddTimed(string partition, string key, object value, DateTime utcExpiry);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        void AddTimed(string key, object value, DateTime utcExpiry);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        Task AddTimedAsync(string partition, string key, object value, DateTime utcExpiry);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        Task AddTimedAsync(string key, object value, DateTime utcExpiry);

        /// <summary>
        ///   TODO
        /// </summary>
        void Clear();

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="cacheReadMode"></param>
        void Clear(CacheReadMode cacheReadMode);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        bool Contains(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        bool Contains(string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        [Pure]
        int Count();

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="cacheReadMode"></param>
        /// <returns></returns>
        [Pure]
        int Count(CacheReadMode cacheReadMode);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        [Pure]
        long LongCount();

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="cacheReadMode"></param>
        /// <returns></returns>
        [Pure]
        long LongCount(CacheReadMode cacheReadMode);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        object Get(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        object Get(string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        Task<object> GetAsync(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        Task<object> GetAsync(string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        CacheItem GetItem(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        CacheItem GetItem(string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        Task<CacheItem> GetItemAsync(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        [Pure]
        Task<CacheItem> GetItemAsync(string key);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        IList<object> GetAll();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        Task<IList<object>> GetAllAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        IList<object> GetPartition(string partition);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        Task<IList<object>> GetPartitionAsync(string partition);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        IList<CacheItem> GetAllItems();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        Task<IList<CacheItem>> GetAllItemsAsync();

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        IList<CacheItem> GetPartitionItems(string partition);

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        [Pure]
        Task<IList<CacheItem>> GetPartitionItemsAsync(string partition);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        void Remove(string partition, string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        void Remove(string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string partition, string key);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Task RemoveAsync(string key);
    }

    /// <summary>
    ///   TODO
    /// </summary>
    /// <typeparam name="TCache"></typeparam>
    /// <typeparam name="TCacheSettings"></typeparam>
    public interface ICache<TCache, out TCacheSettings> : ICache 
        where TCache : class, ICache<TCache, TCacheSettings>, new()
        where TCacheSettings : CacheSettingsBase, new()
    {
        /// <summary>
        ///   Settings for this cache.
        /// </summary>
        TCacheSettings Settings { get; }
    }

    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable, JsonObject]
    public sealed class CacheItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Partition { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime UtcCreation { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? UtcExpiry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public TimeSpan? Interval { get; set; }
    }

    /// <summary>
    ///   TODO
    /// </summary>
    public enum CacheReadMode : byte
    {
        /// <summary>
        ///   TODO
        /// </summary>
        IgnoreExpirationDate = 0,

        /// <summary>
        ///   TODO
        /// </summary>
        ConsiderExpirationDate = 1
    }
}