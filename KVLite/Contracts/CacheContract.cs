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
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Contracts
{
    [ContractClassFor(typeof(ICache))]
    internal abstract class CacheContract : ICache
    {
        public CacheKind Kind
        {
            get
            {
                Contract.Ensures(Enum.IsDefined(typeof(CacheKind), Contract.Result<CacheKind>()));
                return default(CacheKind);
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

        object ICache.this[string key]
        {
            get
            {
                Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
                return default(object);
            }
        }

        public void AddSliding(string partition, string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public void AddSliding(string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public Task AddSlidingAsync(string partition, string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public Task AddSlidingAsync(string key, object value, TimeSpan interval)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public void AddStatic(string partition, string key, object value)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public void AddStatic(string key, object value)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public Task AddStaticAsync(string partition, string key, object value)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public Task AddStaticAsync(string key, object value)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public void AddTimed(string partition, string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public void AddTimed(string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
        }

        public Task AddTimedAsync(string partition, string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public Task AddTimedAsync(string key, object value, DateTime utcExpiry)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Requires<ArgumentNullException>(value != null, ErrorMessages.NullValue);
            //Contract.Requires<ArgumentException>(value.GetType().IsSerializable, ErrorMessages.NotSerializableValue);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public void Clear()
        {
            // Empty, for now.
        }

        public void Clear(CacheReadMode cacheReadMode)
        {
            Contract.Requires<ArgumentException>(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), ErrorMessages.InvalidEnumValue);
        }

        public bool Contains(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(bool);
        }

        public bool Contains(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(bool);
        }

        public int Count()
        {
            return default(int);
        }

        public int Count(CacheReadMode cacheReadMode)
        {
            Contract.Requires<ArgumentException>(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), ErrorMessages.InvalidEnumValue);
            return default(int);
        }

        public long LongCount()
        {
            return default(long);
        }

        public long LongCount(CacheReadMode cacheReadMode)
        {
            Contract.Requires<ArgumentException>(Enum.IsDefined(typeof(CacheReadMode), cacheReadMode), ErrorMessages.InvalidEnumValue);
            return default(long);
        }

        public object Get(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        public object Get(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(object);
        }

        public Task<object> GetAsync(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task<object>>() != null);
            Contract.Ensures(Contract.Result<Task<object>>().Status != TaskStatus.Created);
            return default(Task<object>);
        }

        public Task<object> GetAsync(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task<object>>() != null);
            Contract.Ensures(Contract.Result<Task<object>>().Status != TaskStatus.Created);
            return default(Task<object>);
        }

        public CacheItem GetItem(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        public CacheItem GetItem(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            return default(CacheItem);
        }

        public Task<CacheItem> GetItemAsync(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task<CacheItem>>() != null);
            Contract.Ensures(Contract.Result<Task<CacheItem>>().Status != TaskStatus.Created);
            return default(Task<CacheItem>);
        }

        public Task<CacheItem> GetItemAsync(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task<CacheItem>>() != null);
            Contract.Ensures(Contract.Result<Task<CacheItem>>().Status != TaskStatus.Created);
            return default(Task<CacheItem>);
        }

        public IList<object> GetAll()
        {
            return default(IList<object>);
        }

        public Task<IList<object>> GetAllAsync()
        {
            Contract.Ensures(Contract.Result<Task<IList<object>>>() != null);
            Contract.Ensures(Contract.Result<Task<IList<object>>>().Status != TaskStatus.Created);
            return default(Task<IList<object>>);
        }

        public IList<object> GetPartition(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            return default(IList<object>);
        }

        public Task<IList<object>> GetPartitionAsync(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<Task<IList<object>>>() != null);
            Contract.Ensures(Contract.Result<Task<IList<object>>>().Status != TaskStatus.Created);
            return default(Task<IList<object>>);
        }

        public IList<CacheItem> GetAllItems()
        {
            return default(IList<CacheItem>);
        }

        public Task<IList<CacheItem>> GetAllItemsAsync()
        {
            Contract.Ensures(Contract.Result<Task<IList<CacheItem>>>() != null);
            Contract.Ensures(Contract.Result<Task<IList<CacheItem>>>().Status != TaskStatus.Created);
            return default(Task<IList<CacheItem>>);
        }

        public IList<CacheItem> GetPartitionItems(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            return default(IList<CacheItem>);
        }

        public Task<IList<CacheItem>> GetPartitionItemsAsync(string partition)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Ensures(Contract.Result<Task<IList<CacheItem>>>() != null);
            Contract.Ensures(Contract.Result<Task<IList<CacheItem>>>().Status != TaskStatus.Created);
            return default(Task<IList<CacheItem>>);
        }

        public void Remove(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
        }

        public void Remove(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
        }

        public Task RemoveAsync(string partition, string key)
        {
            Contract.Requires<ArgumentNullException>(partition != null, ErrorMessages.NullPartition);
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }

        public Task RemoveAsync(string key)
        {
            Contract.Requires<ArgumentNullException>(key != null, ErrorMessages.NullKey);
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return default(Task);
        }
    }
}