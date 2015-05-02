// File name: AsyncCacheExtensions.cs
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
using System.Threading.Tasks;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Async extension methods for the <see cref="ICache"/> interface.
    /// </summary>
    public static class AsyncCacheExtensions
    {
        /// <summary>
        ///   Adds a "sliding" value with given partition and key. Value will last as much as
        ///   specified in given interval and, if accessed before expiry, its lifetime will be
        ///   extended by the interval itself.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddSlidingAsync(this ICache cache, string partition, string key, object value, TimeSpan interval)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddSliding(partition, key, value, interval));
        }

        /// <summary>
        ///   Adds a "sliding" value with given key. Value will last as much as specified in given
        ///   interval and, if accessed before expiry, its lifetime will be extended by the interval itself.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="interval">The interval.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddSlidingAsync(this ICache cache, string key, object value, TimeSpan interval)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddSliding(cache.Settings.DefaultPartition, key, value, interval));
        }

        /// <summary>
        ///   Adds a "static" value with given partition and key. Value will last as much as
        ///   specified in <see cref="AbstractCacheSettings.StaticIntervalInDays"/> and, if accessed
        ///   before expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddStaticAsync(this ICache cache, string partition, string key, object value)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddSliding(partition, key, value, cache.Settings.StaticInterval));
        }

        /// <summary>
        ///   Adds a "static" value with given key. Value will last as much as specified in
        ///   <see cref="AbstractCacheSettings.StaticIntervalInDays"/> and, if accessed before
        ///   expiry, its lifetime will be extended by that interval.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddStaticAsync(this ICache cache, string key, object value)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddSliding(cache.Settings.DefaultPartition, key, value, cache.Settings.StaticInterval));
        }

        /// <summary>
        ///   Adds a "timed" value with given partition and key. Value will last until the specified
        ///   time and, if accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddTimedAsync(this ICache cache, string partition, string key, object value, DateTime utcExpiry)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddTimed(partition, key, value, utcExpiry));
        }

        /// <summary>
        ///   Adds a "timed" value with given key. Value will last until the specified time and, if
        ///   accessed before expiry, its lifetime will _not_ be extended.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcExpiry">The UTC expiry.</param>
        /// <returns>The insertion task.</returns>
        public static Task AddTimedAsync(this ICache cache, string key, object value, DateTime utcExpiry)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.AddTimed(cache.Settings.DefaultPartition, key, value, utcExpiry));
        }

        /// <summary>
        ///   Removes the value with given partition and key.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <returns>The removal task.</returns>
        public static Task RemoveAsync(this ICache cache, string partition, string key)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.Remove(partition, key));
        }

        /// <summary>
        ///   Removes the specified key.
        /// </summary>
        /// <param name="cache">The cache.</param>
        /// <param name="key">The key.</param>
        /// <returns>The removal task.</returns>
        public static Task RemoveAsync(this ICache cache, string key)
        {
            Contract.Ensures(Contract.Result<Task>() != null);
            Contract.Ensures(Contract.Result<Task>().Status != TaskStatus.Created);
            return TaskRunner.Run(() => cache.Remove(cache.Settings.DefaultPartition, key));
        }
    }
}
