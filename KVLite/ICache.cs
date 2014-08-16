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

namespace KVLite
{
    public interface ICache
    {
        [Pure]
        object this[string partition, string key] { get; }
        
        [Pure]
        object this[string key] { get; }

        object AddStatic(string partition, string key, object value);

        object AddStatic(string key, object value);

        void Clear();

        void Clear(CacheReadMode cacheReadMode);

        [Pure]
        int Count();
        
        [Pure]
        int Count(CacheReadMode cacheReadMode);
        
        [Pure]
        long LongCount();
        
        [Pure]
        long LongCount(CacheReadMode cacheReadMode);
        
        [Pure]
        object Get(string partition, string key);
        
        [Pure]
        object Get(string key);
        
        [Pure]
        CacheItem GetItem(string partition, string key);
        
        [Pure]
        CacheItem GetItem(string key);
    }

    public interface ICache<TCache> : ICache where TCache : class, ICache<TCache>, new()
    {
    }

    public enum CacheReadMode : byte
    {
        IgnoreExpirationDate = 0,
        ConsiderExpirationDate = 1
    }
}
