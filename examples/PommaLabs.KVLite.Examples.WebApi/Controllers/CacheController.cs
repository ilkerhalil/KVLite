// File name: CacheController.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
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

using PommaLabs.KVLite.WebApi;
using System;
using System.Collections.Generic;
using System.Web.Http;

namespace PommaLabs.KVLite.Examples.WebApi.Controllers
{
    /// <summary>
    ///   Exposes the KVLite cache controller and allows further customizations.
    /// </summary>
    [RoutePrefix("cache")]
    public sealed class CacheController : AbstractCacheController
    {
        /// <summary>
        ///   Injects the <see cref="ICache"/> dependency into the base controller.
        /// </summary>
        /// <param name="cache">The cache.</param>
        public CacheController(ICache cache) : base(cache)
        {
        }

        /// <summary>
        ///   Deletes an item stored in the cache with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        [Route("items/{partition}/{key}")]
        public override void DeleteItem(string partition, string key)
        {
            base.DeleteItem(partition, key);
        }

        /// <summary>
        ///   Deletes all items stored in the cache.
        /// </summary>
        [Route("items")]
        public override void DeleteItems()
        {
            base.DeleteItems();
        }

        /// <summary>
        ///   Deletes all items stored in the cache with given partition.
        /// </summary>
        /// <param name="partition">The partition.</param>
        [Route("items/{partition}")]
        public override void DeletePartitionItems(string partition)
        {
            base.DeletePartitionItems(partition);
        }

        /// <summary>
        ///   Returns a _valid_ item stored in the cache for given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>A _valid_ item stored in the cache for given partition and key.</returns>
        [Route("items/{partition}/{key}")]
        public override ICacheItem<object> GetItem(string partition, string key)
        {
            return base.GetItem(partition, key);
        }

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
        [Route("items")]
        public override IEnumerable<ICacheItem<object>> GetItems(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = default(DateTime?), DateTime? toExpiry = default(DateTime?), DateTime? fromCreation = default(DateTime?), DateTime? toCreation = default(DateTime?))
        {
            return base.GetItems(partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
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
        [Route("items/withValues")]
        public override IEnumerable<ICacheItem<object>> GetItemsWithValues(string partitionLike = null, string keyLike = null, DateTime? fromExpiry = default(DateTime?), DateTime? toExpiry = default(DateTime?), DateTime? fromCreation = default(DateTime?), DateTime? toCreation = default(DateTime?))
        {
            return base.GetItemsWithValues(partitionLike, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
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
        [Route("items/{partition}")]
        public override IEnumerable<ICacheItem<object>> GetPartitionItems(string partition, string keyLike = null, DateTime? fromExpiry = default(DateTime?), DateTime? toExpiry = default(DateTime?), DateTime? fromCreation = default(DateTime?), DateTime? toCreation = default(DateTime?))
        {
            return base.GetPartitionItems(partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
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
        [Route("items/{partition}/withValues")]
        public override IEnumerable<ICacheItem<object>> GetPartitionItemsWithValues(string partition, string keyLike = null, DateTime? fromExpiry = default(DateTime?), DateTime? toExpiry = default(DateTime?), DateTime? fromCreation = default(DateTime?), DateTime? toCreation = default(DateTime?))
        {
            return base.GetPartitionItemsWithValues(partition, keyLike, fromExpiry, toExpiry, fromCreation, toCreation);
        }
    }
}
