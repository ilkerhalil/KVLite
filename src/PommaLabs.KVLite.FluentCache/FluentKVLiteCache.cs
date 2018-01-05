// File name: FluentKVLiteCache.cs
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

using FluentCache;
using FluentCache.Execution;

namespace PommaLabs.KVLite.FluentCache
{
    /// <summary>
    ///   KVLite adapter for FluentCache.
    /// </summary>
    public sealed class FluentKVLiteCache : global::FluentCache.ICache
    {
        /// <summary>
        ///   Creates an execution plan for the specified caching strategy.
        /// </summary>
        /// <param name="cacheStrategy">Caching strategy.</param>
        /// <returns>An execution plan for the specified caching strategy.</returns>
        public ICacheExecutionPlan<T> CreateExecutionPlan<T>(ICacheStrategy<T> cacheStrategy)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///   Gets a value from the cache.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="region">The region in the cache where the key is stored.</param>
        /// <returns>The cached value.</returns>
        public CachedValue<T> Get<T>(string key, string region)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///   Gets a string representation of a parameter value that is used to build up a unique
        ///   cache key for a parametized caching expression.
        /// </summary>
        /// <param name="parameterValue">Parameter value.</param>
        /// <returns>A string representation of given parameter value</returns>
        public string GetParameterCacheKeyValue(object parameterValue)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///   Marks a value in the cache as validated.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="region">The region in the cache where the key is stored.</param>
        public void MarkAsValidated(string key, string region)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///   Removes a value from the cache.
        /// </summary>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="region">The region in the cache where the key is stored.</param>
        public void Remove(string key, string region)
        {
            throw new System.NotImplementedException();
        }

        /// <summary>
        ///   Sets a value in the cache.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="key">The key of the value to retrieve.</param>
        /// <param name="region">The region in the cache where the key is stored</param>
        /// <param name="value">The cached value.</param>
        /// <param name="cacheExpiration">The expiration policy for this value.</param>
        /// <returns>The cached value.</returns>
        public CachedValue<T> Set<T>(string key, string region, T value, CacheExpiration cacheExpiration)
        {
            throw new System.NotImplementedException();
        }
    }
}
