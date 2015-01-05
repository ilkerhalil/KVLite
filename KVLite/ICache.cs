// File name: ICache.cs
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

using PommaLabs.KVLite.Contracts;
using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Represents a partition based key-value store. Each (partition, key, value) triple has
    ///   attached either an expiry time or a refresh interval, because values should not be stored
    ///   forever inside a cache. <br/> In fact, a cache is, almost by definition, a transient
    ///   store, used to temporaly store the results of time consuming operations.
    /// </summary>
    [ContractClass(typeof(CacheContract))]
    public interface ICache
    {
        /// <summary>
        ///   TODO
        /// </summary>
        CacheKind Kind { get; }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        CacheSettingsBase Settings { get; }

        /// <summary>
        ///   Gets the value with the specified partition and key.
        /// </summary>
        /// <value>The value with the specified partition and key.</value>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified partition and key.</returns>
        [Pure]
        object this[string partition, string key] { get; }

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
        ///   </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="utcExpiry"></param>
        void AddTimed(string partition, string key, object value, DateTime utcExpiry);

        /// <summary>
        ///   TODO
        /// </summary>
        void Clear();

        /// <summary>
        ///   TODO
        /// </summary>
        void Clear(string partition);

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
        /// <returns></returns>
        [Pure]
        long LongCount();

        /// <summary>
        ///   TODO
        /// </summary>
        /// <returns></returns>
        [Pure]
        long LongCount(string partition);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        object Get(string partition, string key);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        CacheItem GetItem(string partition, string key);

        /// <summary>
        ///   </summary>
        /// <returns></returns>
        IList<CacheItem> GetManyItems();

        /// <summary>
        ///   </summary>
        /// <returns></returns>
        IList<CacheItem> GetManyItems(string partition);

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        [Pure]
        object Peek(string partition, string key);

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        [Pure]
        CacheItem PeekItem(string partition, string key);

        /// <summary>
        ///   </summary>
        /// <returns></returns>
        [Pure]
        IList<CacheItem> PeekManyItems();

        /// <summary>
        ///   </summary>
        /// <returns></returns>
        [Pure]
        IList<CacheItem> PeekManyItems(string partition);

        /// <summary>
        ///   </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        void Remove(string partition, string key);
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
        ///   The available settings for the cache.
        /// </summary>
        new TCacheSettings Settings { get; }
    }
}