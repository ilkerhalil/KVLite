// File name: CacheContract.cs
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
using System.Diagnostics.Contracts;
using Finsa.CodeServices.Clock;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite.Contracts
{
    /// <summary>
    ///   Contract class for <see cref="ICache"/>.
    /// </summary>
    [ContractClassFor(typeof(ICache))]
    public abstract class CacheContract : ICache
    {
        #region ICache Members

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <value>
        ///   The clock used by the cache.
        /// </value>
        public IClock Clock
        {
            get
            {
                Contract.Ensures(Contract.Result<IClock>() != null);
                return default(IClock);
            }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public CacheSettingsBase Settings
        {
            get
            {
                Contract.Ensures(Contract.Result<CacheSettingsBase>() != null);
                return default(CacheSettingsBase);
            }
        }

        /// <summary>
        ///   Gets the value with the specified partition and key.
        /// </summary>
        /// <value>The value with the specified partition and key.</value>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with the specified partition and key.</returns>
        object ICache.this[string partition, string key]
        {
            get
            {
                Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
                Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
                return default(object);
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        public void Clear()
        {
            Contract.Ensures(this.Count() == 0);
            Contract.Ensures(LongCount() == 0);
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition"></param>
        public void Clear(string partition)
        {
            Contract.Ensures(this.Count(partition) == 0);
            Contract.Ensures(LongCount(partition) == 0);
        }

        /// <summary>
        ///   Determines whether cache contains the specified partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified partition and key.</returns>
        public bool Contains(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(bool);
        }

        /// <summary>
        ///   The number of items in the cache.
        /// </summary>
        /// <returns>The number of items in the cache.</returns>
        public long LongCount()
        {
            Contract.Ensures(Contract.Result<long>() >= 0);
            return default(long);
        }

        /// <summary>
        ///   The number of items in given partition.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>The number of items in given partition.</returns>
        public long LongCount(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<long>() >= 0);
            return default(long);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        public object Get(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        public CacheItem GetItem(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will
        ///   be increased by corresponding interval.
        /// </summary>
        /// <returns>All cache items.</returns>
        public IList<CacheItem> GetManyItems()
        {
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        public IList<CacheItem> GetManyItems(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public object Peek(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public CacheItem PeekItem(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <returns>All values, without updating expiry dates.</returns>
        public IList<CacheItem> PeekManyItems()
        {
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        public IList<CacheItem> PeekManyItems(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        public void Remove(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
        }

        #endregion ICache Members
    }
}