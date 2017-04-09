// File name: AbstractCacheController.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace PommaLabs.KVLite.WebApi
{
    /// <summary>
    ///   Implements some actions to remotely interact with a KVLite cache.
    /// </summary>
    public abstract class AbstractCacheController : ApiController
    {
        /// <summary>
        ///   Initializes a new instance of the <see cref="AbstractCacheController"/> class.
        /// </summary>
        /// <param name="cache">The cache used by the Web API output cache.</param>
        protected AbstractCacheController(ICache cache)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);

            Cache = cache;
        }

        /// <summary>
        ///   Gets the cache used by the controller.
        /// </summary>
        /// <value>The cache used by the controller.</value>
        public ICache Cache { get; }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache which follow given search criteria.
        ///   Values are omitted, in order to keep the response small.
        /// </summary>
        /// <param name="partitionLike">
        ///   Optional, a substring that should be contained in the partition of the items.
        /// </param>
        /// <param name="keyLike">
        ///   Optional, a substring that should be contained in the key of the items.
        /// </param>
        /// <param name="fromExpiry">Optional, the minimum expiry date items should have.</param>
        /// <param name="toExpiry">Optional, the maximum expiry date items should have.</param>
        /// <param name="fromCreation">Optional, the minimum creation date items should have.</param>
        /// <param name="toCreation">Optional, the maximum creation date items should have.</param>
        /// <returns>All _valid_ items stored in the cache which follow given search criteria.</returns>
        /// <remarks>Value is not serialized in the response, since it might be truly heavy.</remarks>
#if !NET40

        [Route("items")]
#endif

        public virtual IEnumerable<ICacheItem<object>> GetItems(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            // Removes the value, as stated in the docs.
            var items = Cache.GetItems<object>().Select(i => new CacheItem<object>(i.Partition, i.Key, null, i.UtcCreation, i.UtcExpiry, i.Interval, i.ParentKeys));
            return QueryCacheItems(items, partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache which follow given search criteria.
        ///   Values are included in the response.
        /// </summary>
        /// <param name="partitionLike">
        ///   Optional, a substring that should be contained in the partition of the items.
        /// </param>
        /// <param name="keyLike">
        ///   Optional, a substring that should be contained in the key of the items.
        /// </param>
        /// <param name="fromExpiry">Optional, the minimum expiry date items should have.</param>
        /// <param name="toExpiry">Optional, the maximum expiry date items should have.</param>
        /// <param name="fromCreation">Optional, the minimum creation date items should have.</param>
        /// <param name="toCreation">Optional, the maximum creation date items should have.</param>
        /// <returns>All _valid_ items stored in the cache which follow given search criteria.</returns>
#if !NET40

        [Route("items/withValues")]
#endif

        public virtual IEnumerable<ICacheItem<object>> GetItemsWithValues(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            return QueryCacheItems(Cache.GetItems<object>(), partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Deletes all items stored in the cache.
        /// </summary>
#if !NET40

        [Route("items")]
#endif

        public virtual void DeleteItems()
        {
            Cache.Clear();
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition which follow given
        ///   search criteria. Values are omitted, in order to keep the response small.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="keyLike">
        ///   Optional, a substring that should be contained in the key of the items.
        /// </param>
        /// <param name="fromExpiry">Optional, the minimum expiry date items should have.</param>
        /// <param name="toExpiry">Optional, the maximum expiry date items should have.</param>
        /// <param name="fromCreation">Optional, the minimum creation date items should have.</param>
        /// <param name="toCreation">Optional, the maximum creation date items should have.</param>
        /// <returns>
        ///   All _valid_ items stored in the cache for given partition which follow given search criteria.
        /// </returns>
        /// <remarks>Value is not serialized in the response, since it might be truly heavy.</remarks>
#if !NET40

        [Route("items/{partition}")]
#endif

        public virtual IEnumerable<ICacheItem<object>> GetPartitionItems(string partition, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            // Removes the value, as stated in the docs.
            var items = Cache.GetItems<object>(partition).Select(i => new CacheItem<object>(i.Partition, i.Key, null, i.UtcCreation, i.UtcExpiry, i.Interval, i.ParentKeys));
            return QueryCacheItems(items, partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Returns all _valid_ items stored in the cache for given partition which follow given
        ///   search criteria. Values are included in the response.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="keyLike">
        ///   Optional, a substring that should be contained in the key of the items.
        /// </param>
        /// <param name="fromExpiry">Optional, the minimum expiry date items should have.</param>
        /// <param name="toExpiry">Optional, the maximum expiry date items should have.</param>
        /// <param name="fromCreation">Optional, the minimum creation date items should have.</param>
        /// <param name="toCreation">Optional, the maximum creation date items should have.</param>
        /// <returns>
        ///   All _valid_ items stored in the cache for given partition which follow given search criteria.
        /// </returns>
#if !NET40

        [Route("items/{partition}/withValues")]
#endif

        public virtual IEnumerable<ICacheItem<object>> GetPartitionItemsWithValues(string partition, string keyLike = null, DateTime? fromExpiry = null, DateTime? toExpiry = null, DateTime? fromCreation = null, DateTime? toCreation = null)
        {
            return QueryCacheItems(Cache.GetItems<object>(partition), partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }

        /// <summary>
        ///   Deletes all items stored in the cache with given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
#if !NET40

        [Route("items/{partition}")]
#endif

        public virtual void DeletePartitionItems(string partition) => Cache.Clear(partition);

        /// <summary>
        ///   Returns a _valid_ item stored in the cache for given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>A _valid_ item stored in the cache for given partition and key.</returns>
#if !NET40

        [Route("items/{partition}/{key}")]
#endif

        public virtual ICacheItem<object> GetItem(string partition, string key) => Cache.GetItem<object>(partition, key).ValueOrDefault();

        /// <summary>
        ///   Deletes an item stored in the cache with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
#if !NET40

        [Route("items/{partition}/{key}")]
#endif

        public virtual void DeleteItem(string partition, string key) => Cache.Remove(partition, key);

        /// <summary>
        ///   Common method used for querying items.
        /// </summary>
        /// <param name="items">The items on which the query should be performed.</param>
        /// <param name="partitionLike">
        ///   Optional, a substring that should be contained in the partition of the items.
        /// </param>
        /// <param name="keyLike">
        ///   Optional, a substring that should be contained in the key of the items.
        /// </param>
        /// <param name="fromExpiry">Optional, the minimum expiry date items should have.</param>
        /// <param name="toExpiry">Optional, the maximum expiry date items should have.</param>
        /// <param name="fromCreation">Optional, the minimum creation date items should have.</param>
        /// <param name="toCreation">Optional, the maximum creation date items should have.</param>
        /// <returns>The items extracted by the query.</returns>
        private static IEnumerable<ICacheItem<object>> QueryCacheItems(IEnumerable<ICacheItem<object>> items, string partitionLike, string keyLike, DateTime? fromExpiry, DateTime? toExpiry, DateTime? fromCreation, DateTime? toCreation)
        {
            var fromExpiryUnix = fromExpiry.HasValue ? fromExpiry.Value.ToUniversalTime().ToUnixTime() : new long?();
            var toExpiryUnix = toExpiry.HasValue ? toExpiry.Value.ToUniversalTime().ToUnixTime() : new long?();
            var fromCreationUnix = fromCreation.HasValue ? fromCreation.Value.ToUniversalTime().ToUnixTime() : new long?();
            var toCreationUnix = toCreation.HasValue ? toCreation.Value.ToUniversalTime().ToUnixTime() : new long?();

            return from i in items
                   where string.IsNullOrWhiteSpace(partitionLike) || i.Partition.Contains(partitionLike)
                   where string.IsNullOrWhiteSpace(keyLike) || i.Key.Contains(keyLike)
                   where !fromExpiryUnix.HasValue || i.UtcExpiry.ToUnixTime() >= fromExpiryUnix.Value
                   where !toExpiryUnix.HasValue || i.UtcExpiry.ToUnixTime() <= toExpiryUnix.Value
                   where !fromCreationUnix.HasValue || i.UtcCreation.ToUnixTime() >= fromCreationUnix.Value
                   where !toCreationUnix.HasValue || i.UtcCreation.ToUnixTime() <= toCreationUnix.Value
                   select i;
        }
    }
}
