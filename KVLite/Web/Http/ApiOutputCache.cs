﻿// File name: ApiOutputCache.cs
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
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Web.Http;
using PommaLabs.KVLite.Properties;
using WebApi.OutputCache.Core.Cache;

#if NET45

using WebApi.OutputCache.V2;

#else

using WebAPI.OutputCache;

#endif

namespace PommaLabs.KVLite.Web.Http
{
    /// <summary>
    ///   KVLite-based output cache for Wev API.
    /// </summary>
    public sealed class ApiOutputCache : IApiOutputCache
    {
        #region Fields

        private readonly ICache _cache;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Initializes a new instance of the <see cref="ApiOutputCache"/> class.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public ApiOutputCache(ICache cache)
        {
            Contract.Requires<ArgumentNullException>(cache != null);
            _cache = cache;
        }

        #endregion

        #region Public Members

        /// <summary>
        ///   Gets the underlying cache.
        /// </summary>
        /// <value>
        ///   The underlying cache.
        /// </value>
        public ICache Cache
        {
            get { return _cache; }
        }

        /// <summary>
        ///   Registers this class as the default API output cache provider. Please use
        ///   <see cref="Settings.Web_Http_ApiOutputCacheProviderType"/> to customize the cache kind
        ///   and the partition name.
        /// </summary>
        /// <param name="configuration">The Web API configuration instance.</param>
        /// <param name="cache">The underlying cache.</param>
        public static void RegisterAsCacheOutputProvider(HttpConfiguration configuration, ICache cache)
        {
            configuration.CacheOutputConfiguration().RegisterCacheOutputProvider(() => new ApiOutputCache(cache));
        }

        #endregion Public Members

        #region IApiOutputCache Members

#pragma warning disable 1591

        public IEnumerable<string> AllKeys
        {
            get { return _cache.GetManyItems(Settings.Default.Web_Http_ApiOutputCacheProviderPartition).Select(i => i.Key); }
        }

        public void RemoveStartsWith(string key)
        {
            var items = _cache.GetManyItems(Settings.Default.Web_Http_ApiOutputCacheProviderPartition);
            foreach (var i in items.Where(item => item.Key.StartsWith(key)))
            {
                Debug.Assert(i.Partition == Settings.Default.Web_Http_ApiOutputCacheProviderPartition);
                _cache.Remove(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, i.Key);
            }
        }

        public T Get<T>(string key) where T : class
        {
            return _cache.Get<T>(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, key);
        }

        public object Get(string key)
        {
            return _cache.Get(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, key);
        }

        public void Remove(string key)
        {
            _cache.Remove(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, key);
        }

        public bool Contains(string key)
        {
            return _cache.Contains(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, key);
        }

        public void Add(string key, object o, DateTimeOffset expiration, string dependsOnKey = null)
        {
            // KVLite does not support dependency handling; therefore, we ignore the dependsOnKey parameter.
            _cache.AddTimed(Settings.Default.Web_Http_ApiOutputCacheProviderPartition, key, o, expiration.UtcDateTime);
        }

#pragma warning restore 1591

        #endregion IApiOutputCache Members
    }
}