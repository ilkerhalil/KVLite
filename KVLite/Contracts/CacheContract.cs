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
using System.Diagnostics.Contracts;
using Common.Logging;
using Finsa.CodeServices.Clock;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using FSharpx;
using Microsoft.FSharp.Core;
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
        /// <value>The clock used by the cache.</value>
        public IClock Clock
        {
            get
            {
                Contract.Ensures(Contract.Result<IClock>() != null);
                return default(IClock);
            }
        }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <value>The compressor used by the cache.</value>
        public ICompressor Compressor
        {
            get
            {
                Contract.Ensures(Contract.Result<ICompressor>() != null);
                return default(ICompressor);
            }
        }

        /// <summary>
        ///   Gets the log used by the cache.
        /// </summary>
        /// <value>The log used by the cache.</value>
        public ILog Log
        {
            get
            {
                Contract.Ensures(Contract.Result<ILog>() != null);
                return default(ILog);
            }
        }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <value>The serializer used by the cache.</value>
        public ISerializer Serializer
        {
            get
            {
                Contract.Ensures(Contract.Result<ISerializer>() != null);
                return default(ISerializer);
            }
        }

        /// <summary>
        ///   The available settings for the cache.
        /// </summary>
        public AbstractCacheSettings Settings
        {
            get
            {
                Contract.Ensures(Contract.Result<AbstractCacheSettings>() != null);
                return default(AbstractCacheSettings);
            }
        }

        FSharpOption<object> ICache.this[string partition, string key]
        {
            get
            {
                Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
                Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
                Contract.Ensures(Contains(partition, key) == Contract.Result<FSharpOption<object>>().HasValue());
                return default(FSharpOption<object>);
            }
        }

        FSharpOption<object> ICache.this[string key]
        {
            get
            {
                Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
                Contract.Ensures(Contains(Settings.DefaultPartition, key) == Contract.Result<FSharpOption<object>>().HasValue());
                Contract.Ensures(Contains(key) == Contract.Result<FSharpOption<object>>().HasValue());
                return default(FSharpOption<object>);
            }
        }

        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <typeparam name="TVal">The type of the expected value.</typeparam>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        public void AddSliding<TVal>(string partition, string key, TVal value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            // Contract.Ensures(Contains(partition, key));
        }

        public void AddSliding<TVal>(string key, TVal value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contains(Settings.DefaultPartition, key));
            // Contract.Ensures(Contains(key));
        }

        public void AddStatic<TVal>(string partition, string key, TVal value)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            // Contract.Ensures(Contains(partition, key));
        }

        public void AddStatic<TVal>(string key, TVal value)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            // Contract.Ensures(Contains(key));
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        public void AddTimed<TVal>(string partition, string key, TVal value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            // Contract.Ensures(Contains(partition, key));
        }

        public void AddTimed<TVal>(string key, TVal value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentException>(value == null || (Serializer.CanSerialize(value.GetType()) && Serializer.CanDeserialize(value.GetType())), ErrorMessages.NotSerializableValue);
            // Contract.Ensures(Contains(key));
        }

        /// <summary>
        ///   Clears this instance, that is, it removes all values.
        /// </summary>
        public void Clear()
        {
            Contract.Ensures(Count() == 0);
            Contract.Ensures(LongCount() == 0);
        }

        /// <summary>
        ///   Clears given partition, that is, it removes all its values.
        /// </summary>
        /// <param name="partition"></param>
        public void Clear(string partition)
        {
            Contract.Ensures(Count(partition) == 0);
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
        ///   Determines whether cache contains the specified key in the default partition.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>Whether cache contains the specified key in the default partition.</returns>
        /// <remarks>Calling this method does not extend sliding items lifetime.</remarks>
        /// <exception cref="ArgumentNullException">Key is null.</exception>
        public bool Contains(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(bool);
        }

        public int Count()
        {
            Contract.Ensures(Contract.Result<int>() >= 0);
            return default(int);
        }

        public int Count(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<int>() >= 0);
            return default(int);
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
        public FSharpOption<TVal> Get<TVal>(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(partition, key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            return default(FSharpOption<TVal>);
        }

        /// <summary>
        ///   Gets the value with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The value with specified partition and key.</returns>
        public FSharpOption<TVal> Get<TVal>(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(Settings.DefaultPartition, key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            Contract.Ensures(Contains(key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            return default(FSharpOption<TVal>);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        public FSharpOption<CacheItem<TVal>> GetItem<TVal>(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(partition, key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            return default(FSharpOption<CacheItem<TVal>>);
        }

        /// <summary>
        ///   Gets the cache item with specified partition and key. If it is a "sliding" or "static"
        ///   value, its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The cache item with specified partition and key.</returns>
        public FSharpOption<CacheItem<TVal>> GetItem<TVal>(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(Settings.DefaultPartition, key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            Contract.Ensures(Contains(key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            return default(FSharpOption<CacheItem<TVal>>);
        }

        /// <summary>
        ///   Gets all cache items. If an item is a "sliding" or "static" value, its lifetime will
        ///   be increased by corresponding interval.
        /// </summary>
        /// <returns>All cache items.</returns>
        public CacheItem<TVal>[] GetItems<TVal>()
        {
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>() != null);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().Length == Count());
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().LongLength == LongCount());
            return default(CacheItem<TVal>[]);
        }

        /// <summary>
        ///   Gets all cache items in given partition. If an item is a "sliding" or "static" value,
        ///   its lifetime will be increased by corresponding interval.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <returns>All cache items in given partition.</returns>
        public CacheItem<TVal>[] GetItems<TVal>(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>() != null);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().Length == Count(partition));
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().LongLength == LongCount(partition));
            return default(CacheItem<TVal>[]);
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<TVal> Peek<TVal>(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(partition, key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            return default(FSharpOption<TVal>);
        }

        /// <summary>
        ///   Gets the value corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The value corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<TVal> Peek<TVal>(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(Settings.DefaultPartition, key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            Contract.Ensures(Contains(key) == Contract.Result<FSharpOption<TVal>>().HasValue());
            return default(FSharpOption<TVal>);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<CacheItem<TVal>> PeekItem<TVal>(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(partition, key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            return default(FSharpOption<CacheItem<TVal>>);
        }

        /// <summary>
        ///   Gets the item corresponding to given partition and key, without updating expiry date.
        /// </summary>
        /// <param name="partition"></param>
        /// <param name="key"></param>
        /// <returns>
        ///   The item corresponding to given partition and key, without updating expiry date.
        /// </returns>
        public FSharpOption<CacheItem<TVal>> PeekItem<TVal>(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contains(Settings.DefaultPartition, key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            Contract.Ensures(Contains(key) == Contract.Result<FSharpOption<CacheItem<TVal>>>().HasValue());
            return default(FSharpOption<CacheItem<TVal>>);
        }

        /// <summary>
        ///   Gets the all values, without updating expiry dates.
        /// </summary>
        /// <returns>All values, without updating expiry dates.</returns>
        public CacheItem<TVal>[] PeekItems<TVal>()
        {
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>() != null);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().Length == Count());
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().LongLength == LongCount());
            return default(CacheItem<TVal>[]);
        }

        /// <summary>
        ///   Gets the all items in given partition, without updating expiry dates.
        /// </summary>
        /// <param name="partition"></param>
        /// <returns>All items in given partition, without updating expiry dates.</returns>
        public CacheItem<TVal>[] PeekItems<TVal>(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>() != null);
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().Length == Count(partition));
            Contract.Ensures(Contract.Result<CacheItem<TVal>[]>().LongLength == LongCount(partition));
            return default(CacheItem<TVal>[]);
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
            Contract.Ensures(!Contains(partition, key));
        }

        public void Remove(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(!Contains(Settings.DefaultPartition, key));
            Contract.Ensures(!Contains(key));
        }

        #endregion ICache Members
    }
}
