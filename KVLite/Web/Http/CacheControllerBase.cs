// File name: CacheControllerBase.cs
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

using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using Finsa.CodeServices.Common;
using LinqToQuerystring.WebApi;

#if NET45

using WebApi.OutputCache.V2;

#else

using WebAPI.OutputCache;

#endif

namespace PommaLabs.KVLite.Web.Http
{
    /// <summary>
    ///   Implements some actions to remotely interact with a KVLite cache.
    /// </summary>
    public abstract class CacheControllerBase : ApiController
    {
        private static readonly IQueryable<CacheItem<object>> NoItems = new List<CacheItem<object>>().AsQueryable();

        /// <summary>
        ///   Returns all _valid_ items stored in the cache. Values are omitted, in order to keep
        ///   the response small.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache.</returns>
        /// <remarks>Value is not serialized in the response, since it might be truly heavy.</remarks>
#if NET45

        [Route("items")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem<object>> GetItems()
        {
            var apiOutputCache = GetApiOutputCache();
            if (apiOutputCache == null)
            {
                return NoItems;
            }
            var items = apiOutputCache.GetItems<object>();
            foreach (var item in items)
            {
                item.Value = null; // Removes the value, as stated in the docs.
            }
            return items.AsQueryable();
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache. Values are included in the response.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache.</returns>
#if NET45

        [Route("items/withValues")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem<object>> GetItemsWithValues()
        {
            var apiOutputCache = GetApiOutputCache();
            return (apiOutputCache == null) ? NoItems : apiOutputCache.GetItems<object>().AsQueryable();
        }

        /// <summary>
        ///   Deletes all items stored in the cache.
        /// </summary>
#if NET45

        [Route("items")]
#endif
        public virtual void DeleteItems()
        {
            var apiOutputCache = GetApiOutputCache();
            if (apiOutputCache != null)
            {
                apiOutputCache.Clear();
            }
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition. Values are omitted,
        ///   in order to keep the response small.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache for given partition.</returns>
        /// <remarks>Value is not serialized in the response, since it might be truly heavy.</remarks>
#if NET45

        [Route("items/{partition}")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem<object>> GetItems(string partition)
        {
            var apiOutputCache = GetApiOutputCache();
            if (apiOutputCache == null)
            {
                return NoItems;
            }
            var items = apiOutputCache.GetItems<object>(partition);
            foreach (var item in items)
            {
                item.Value = null; // Removes the value, as stated in the docs.
            }
            return items.AsQueryable();
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition. Values are included
        ///   in the response.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache for given partition.</returns>
#if NET45

        [Route("items/{partition}/withValues")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem<object>> GetItemsWithValues(string partition)
        {
            var apiOutputCache = GetApiOutputCache();
            return (apiOutputCache == null) ? NoItems : apiOutputCache.GetItems<object>(partition).AsQueryable();
        }

        /// <summary>
        ///   Deletes all items stored in the cache with given partition.
        /// </summary>
#if NET45

        [Route("items/{partition}")]
#endif
        public virtual void DeleteItems(string partition)
        {
            var apiOutputCache = GetApiOutputCache();
            if (apiOutputCache != null)
            {
                apiOutputCache.Clear(partition);
            }
        }

        /// <summary>
        ///   Returns a _valid_ items stored in the cache for given partition and key.
        /// </summary>
        /// <returns>A _valid_ items stored in the cache for given partition and key.</returns>
#if NET45

        [Route("items/{partition}/{key}")]
#endif
        public virtual Option<CacheItem<object>> GetItem(string partition, string key)
        {
            var apiOutputCache = GetApiOutputCache();
            return (apiOutputCache == null) ? Option.None<CacheItem<object>>() : apiOutputCache.GetItem<object>(partition, key);
        }

        /// <summary>
        ///   Deletes an items stored in the cache with given partition and key.
        /// </summary>
#if NET45

        [Route("items/{partition}/{key}")]
#endif
        public virtual void DeleteItem(string partition, string key)
        {
            var apiOutputCache = GetApiOutputCache();
            if (apiOutputCache != null)
            {
                apiOutputCache.Remove(partition, key);
            }
        }

        private ICache GetApiOutputCache()
        {
            var cacheOutputConfiguration = Configuration.CacheOutputConfiguration();
            var apiOutputCache = cacheOutputConfiguration.GetCacheOutputProvider(Request) as ApiOutputCache;
            return (apiOutputCache == null) ? null : apiOutputCache.Cache;
        }
    }
}
