// File name: AbstractCacheController.cs
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
using System.Linq;
using System.Web.Http;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Common;
using PommaLabs.Thrower;

namespace PommaLabs.KVLite.Web.Http
{
    /// <summary>
    ///   Implements some actions to remotely interact with a KVLite cache.
    /// </summary>
    public abstract class AbstractCacheController : ApiController
    {
        private readonly ICache _cache;

        /// <summary>
        ///   Initializes a new instance of the <see cref="AbstractCacheController"/> class.
        /// </summary>
        /// <param name="cache">The cache used by the Web API output cache.</param>
        protected AbstractCacheController(ICache cache)
        {
            // Preconditions
            Raise<ArgumentNullException>.IfIsNull(cache);

            _cache = cache;
        }

        /// <summary>
        ///   Gets the cache used by the controller.
        /// </summary>
        /// <value>The cache used by the controller.</value>
        public ICache Cache
        {
            get { return _cache; }
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache. Values are omitted, in order to keep
        ///   the response small.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache.</returns>
        /// <remarks>Value is not serialized in the response, since it might be truly heavy.</remarks>
#if NET45

        [Route("items")]
#endif
        public virtual IEnumerable<CacheItem<object>> GetItems(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            var items = _cache.GetItems<object>();
            foreach (var item in items)
            {
                item.Value = null; // Removes the value, as stated in the docs.
            }
            return QueryCacheItems(items, partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache. Values are included in the response.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache.</returns>
#if NET45

        [Route("items/withValues")]
#endif
        public virtual IEnumerable<CacheItem<object>> GetItemsWithValues(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            return QueryCacheItems(_cache.GetItems<object>(), partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Deletes all items stored in the cache.
        /// </summary>
#if NET45

        [Route("items")]
#endif
        public virtual void DeleteItems()
        {
            _cache.Clear();
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
        public virtual IEnumerable<CacheItem<object>> GetPartitionItems(string partition, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            var items = _cache.GetItems<object>(partition);
            foreach (var item in items)
            {
                item.Value = null; // Removes the value, as stated in the docs.
            }
            return QueryCacheItems(items, partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition. Values are included
        ///   in the response.
        /// </summary>
        /// <returns>All _valid_ items stored in the cache for given partition.</returns>
#if NET45

        [Route("items/{partition}/withValues")]
#endif
        public virtual IEnumerable<CacheItem<object>> GetPartitionItemsWithValues(string partition, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            return QueryCacheItems(_cache.GetItems<object>(partition), partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Deletes all items stored in the cache with given partition.
        /// </summary>
#if NET45

        [Route("items/{partition}")]
#endif
        public virtual void DeletePartitionItems(string partition)
        {
            _cache.Clear(partition);
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
            return _cache.GetItem<object>(partition, key);
        }

        /// <summary>
        ///   Deletes an items stored in the cache with given partition and key.
        /// </summary>
#if NET45

        [Route("items/{partition}/{key}")]
#endif
        public virtual void DeleteItem(string partition, string key)
        {
            _cache.Remove(partition, key);
        }

        private IEnumerable<CacheItem<object>> QueryCacheItems(IEnumerable<CacheItem<object>> items, string partitionLike, string keyLike, DateTime? fromExpiry, DateTime? toExpiry, DateTime? fromCreation, DateTime? toCreation)
        {
            if (fromExpiry.HasValue)
            {
                fromExpiry = fromExpiry.Value.ToUniversalTime();
            }
            if (toExpiry.HasValue)
            {
                toExpiry = toExpiry.Value.ToUniversalTime();
            }
            if (fromCreation.HasValue)
            {
                fromCreation = fromCreation.Value.ToUniversalTime();
            }
            if (toCreation.HasValue)
            {
                toCreation = toCreation.Value.ToUniversalTime();
            }
            return from i in items
                   where String.IsNullOrWhiteSpace(partitionLike) || i.Partition.Contains(partitionLike)
                   where String.IsNullOrWhiteSpace(keyLike) || i.Key.Contains(keyLike)
                   where !fromExpiry.HasValue || i.UtcExpiry.ToUnixTime() >= fromExpiry.Value.ToUnixTime()
                   where !toExpiry.HasValue || i.UtcExpiry.ToUnixTime() <= toExpiry.Value.ToUnixTime()
                   where !fromCreation.HasValue || i.UtcCreation.ToUnixTime() >= fromCreation.Value.ToUnixTime()
                   where !toCreation.HasValue || i.UtcCreation.ToUnixTime() <= toCreation.Value.ToUnixTime()
                   select i;
        }
    }
}