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

using System.Diagnostics.Contracts;
using System.Threading.Tasks;

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    public interface ICache
    {
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
        /// <returns></returns>
        object AddStatic(string partition, string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        object AddStatic(string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<object> AddStaticAsync(string partition, string key, object value);

        /// <summary>
        ///   TODO
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Task<object> AddStaticAsync(string key, object value);

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
    }

    /// <summary>
    ///   TODO
    /// </summary>
    /// <typeparam name="TCache"></typeparam>
    public interface ICache<TCache> : ICache where TCache : class, ICache<TCache>, new()
    {
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
