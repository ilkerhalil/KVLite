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

using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using LinqToQuerystring.WebApi;

namespace PommaLabs.KVLite.Web.Http
{
    /// <summary>
    ///   Implements some actions to remotely interact with a KVLite cache.
    /// </summary>
    public abstract class CacheControllerBase : ApiController
    {
        /// <summary>
        ///   Returns all _valid_ items stored in the cache.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache.</returns>
#if NET45

        [Route("items")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem> GetItems()
        {
            return ApiOutputCache.Cache.GetManyItems().AsQueryable();
        }

        /// <summary>
        ///   Deletes all items stored in the cache.
        /// </summary>
#if NET45

        [Route("items")]
#endif
        public virtual HttpResponseMessage DeleteItems()
        {
            ApiOutputCache.Cache.Clear();
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache for given partition.</returns>
#if NET45

        [Route("items/{partition}")]
#endif
        [LinqToQueryable]
        public virtual IQueryable<CacheItem> GetItems(string partition)
        {
            return ApiOutputCache.Cache.GetManyItems(partition).AsQueryable();
        }

        /// <summary>
        ///   Deletes all items stored in the cache with given partition.
        /// </summary>
#if NET45

        [Route("items/{partition}")]
#endif
        public virtual HttpResponseMessage DeleteItems(string partition)
        {
            ApiOutputCache.Cache.Clear(partition);
            return Request.CreateResponse(HttpStatusCode.OK);
        }

        /// <summary>
        ///   Returns a _valid_ items stored in the cache for given partition and key.
        /// </summary>
        /// <returns>A _valid_ items stored in the cache for given partition and key.</returns>
#if NET45

        [Route("items/{partition}/{key}")]
#endif
        [LinqToQueryable]
        public virtual CacheItem GetItem(string partition, string key)
        {
            return ApiOutputCache.Cache.GetItem(partition, key);
        }

        /// <summary>
        ///   Deletes an items stored in the cache with given partition and key.
        /// </summary>
#if NET45

        [Route("items/{partition}/{key}")]
#endif
        public virtual HttpResponseMessage DeleteItem(string partition, string key)
        {
            ApiOutputCache.Cache.Remove(partition, key);
            return Request.CreateResponse(HttpStatusCode.OK);
        }
    }
}