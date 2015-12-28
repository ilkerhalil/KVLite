﻿// File name: ApiOutputCache.cs
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

using Finsa.CodeServices.Common;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Http;
using WebApi.OutputCache.Core.Cache;

#if NET40

using WebAPI.OutputCache;

#else

using WebApi.OutputCache.V2;

#endif

namespace PommaLabs.KVLite.WebApi
{
    /// <summary>
    ///   KVLite-based output cache for Wev API.
    /// </summary>
    public sealed class ApiOutputCache : IApiOutputCache
    {
        #region Fields

        /// <summary>
        ///   The partition used by Web API output cache provider items.
        /// </summary>
        public const string ResponseCachePartition = "KVLite.Web.Http.ApiOutputCache";

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="ApiOutputCache"/> class.
        /// </summary>
        /// <param name="cache">The cache that will be used as entry container.</param>
        public ApiOutputCache(ICache cache)
        {
            RaiseArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        #endregion Construction

        #region Public Members

        /// <summary>
        ///   Gets the underlying cache.
        /// </summary>
        /// <value>The underlying cache.</value>
        public ICache Cache { get; }

        /// <summary>
        ///   Registers this class as the default API output cache provider.
        /// </summary>
        /// <param name="configuration">The Web API configuration instance.</param>
        /// <param name="cache">The underlying cache.</param>
        public static void RegisterAsCacheOutputProvider(HttpConfiguration configuration, ICache cache = null)
        {
            configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new ApiOutputCache(cache));
        }

        #endregion Public Members

        #region IApiOutputCache Members

#pragma warning disable 1591

        public IEnumerable<string> AllKeys => Cache.GetItems<object>(ResponseCachePartition).Select(i => i.Key);

        public void RemoveStartsWith(string key)
        {
            var items = Cache.GetItems<object>(ResponseCachePartition);
            foreach (var i in items.Where(item => item.Key.StartsWith(key, StringComparison.Ordinal)))
            {
                Debug.Assert(i.Partition == ResponseCachePartition);
                Cache.Remove(ResponseCachePartition, i.Key);
            }
        }

        public T Get<T>(string key) where T : class => Cache.Get<T>(ResponseCachePartition, key).ValueOrDefault();

        public object Get(string key) => Cache.Get<object>(ResponseCachePartition, key).ValueOrDefault();

        public void Remove(string key) => Cache.Remove(ResponseCachePartition, key);

        public bool Contains(string key) => Cache.Contains(ResponseCachePartition, key);

        public void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            // KVLite does not support dependency handling; therefore, we ignore the dependsOnKey parameter.
            Cache.AddTimed(ResponseCachePartition, key, o, expiration.UtcDateTime);
        }

#pragma warning restore 1591

        #endregion IApiOutputCache Members
    }
}