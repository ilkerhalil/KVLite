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

using PommaLabs.KVLite.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace PommaLabs.KVLite.Contracts
{
    /// <summary>
    ///   Contract class for <see cref="ICache"/>.
    /// </summary>
    [ContractClassFor(typeof(ICache))]
    public abstract class CacheContract : ICache
    {
        #region ICache Members

        public CacheSettingsBase Settings
        {
            get
            {
                Contract.Ensures(Contract.Result<CacheSettingsBase>() != null);
                return default(CacheSettingsBase);
            }
        }

        object ICache.this[string partition, string key]
        {
            get
            {
                Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
                Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
                return default(object);
            }
        }

        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public void Clear()
        {
            Contract.Ensures(this.Count() == 0);
            Contract.Ensures(LongCount() == 0);
        }

        public void Clear(string partition)
        {
            Contract.Ensures(this.Count(partition) == 0);
            Contract.Ensures(LongCount(partition) == 0);
        }

        public bool Contains(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(bool);
        }

        public long LongCount()
        {
            Contract.Ensures(Contract.Result<long>() >= 0);
            return default(long);
        }

        public long LongCount(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<long>() >= 0);
            return default(long);
        }

        public object Get(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        public CacheItem GetItem(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        public IList<CacheItem> GetManyItems()
        {
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        public IList<CacheItem> GetManyItems(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        public object Peek(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        public CacheItem PeekItem(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        public IList<CacheItem> PeekManyItems()
        {
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        public IList<CacheItem> PeekManyItems(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<IList<CacheItem>>() != null);
            return default(IList<CacheItem>);
        }

        public void Remove(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
        }

        #endregion
    }
}