// File name: CacheProvider.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

using EntityFramework.Caching;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.EntityFramework
{
    public sealed class CacheProvider : ICacheProvider
    {
        #region Constants

        /// <summary>
        ///   The partition used by EF cache provider items.
        /// </summary>
        const string EfCachePartition = "KVLite.EntityFramework.CacheProvider";

        #endregion Constants

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="CacheProvider"/> class.
        /// </summary>
        /// <param name="cache">The cache that will be used as entry container.</param>
        public CacheProvider(ICache cache)
        {
            RaiseArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        #endregion Construction

        #region Public members

        /// <summary>
        ///   Gets the underlying cache.
        /// </summary>
        /// <value>The underlying cache.</value>
        public ICache Cache { get; }

        #endregion Public members

        #region ICacheProvider members

        public bool Add(CacheKey cacheKey, object value, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public long ClearCache() => Cache.Clear(EfCachePartition);

        public int Expire(CacheTag cacheTag)
        {
            throw new NotImplementedException();
        }

        public object Get(CacheKey cacheKey)
        {
            throw new NotImplementedException();
        }

        public object GetOrAdd(CacheKey cacheKey, Func<CacheKey, object> valueFactory, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public Task<object> GetOrAddAsync(CacheKey cacheKey, Func<CacheKey, Task<object>> valueFactory, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        public object Remove(CacheKey cacheKey)
        {
            throw new NotImplementedException();
        }

        public bool Set(CacheKey cacheKey, object value, CachePolicy cachePolicy)
        {
            throw new NotImplementedException();
        }

        #endregion ICacheProvider members
    }
}
