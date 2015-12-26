// File name: AbstractOutputCacheProvider.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

using Finsa.CodeServices.Common;
using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Web.Caching;

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   Abstract class for KVLite-based output cache providers.
    /// </summary>
    public abstract class AbstractOutputCacheProvider : OutputCacheProvider
    {
        /// <summary>
        ///   The partition used by output cache provider items.
        /// </summary>
        public const string OutputCachePartition = "KVLite.Web.OutputCache";

        /// <summary>
        ///   Initializes the provider using the specified cache.
        /// </summary>
        /// <param name="cache">The cache that will be used by the provider.</param>
        protected AbstractOutputCacheProvider(ICache cache)
        {
            RaiseArgumentNullException.IfIsNull(cache, nameof(cache), ErrorMessages.NullCache);
            Cache = cache;
        }

        /// <summary>
        ///   The cache used by the provider.
        /// </summary>
        public ICache Cache { get; }

        /// <summary>
        ///   Returns a reference to the specified entry in the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for a cached entry in the output cache.</param>
        /// <returns>
        ///   The <paramref name="key"/> value that identifies the specified entry in the cache, or
        ///   null if the specified entry is not in the cache.
        /// </returns>
        public override object Get(string key) => Cache.Get<object>(OutputCachePartition, key).ValueOrDefault();

        /// <summary>
        ///   Inserts the specified entry into the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry"/>.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires.</param>
        /// <returns>A reference to the specified provider.</returns>
        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            Cache.AddTimed(OutputCachePartition, key, entry, utcExpiry);
            return entry;
        }

        /// <summary>
        ///   Inserts the specified entry into the output cache, overwriting the entry if it is
        ///   already cached.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry"/>.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">
        ///   The time and date on which the cached <paramref name="entry"/> expires.
        /// </param>
        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            Cache.AddTimed(OutputCachePartition, key, entry, utcExpiry);
        }

        /// <summary>
        ///   Removes the specified entry from the output cache.
        /// </summary>
        /// <param name="key">
        ///   The unique identifier for the entry to remove from the output cache.
        /// </param>
        public override void Remove(string key)
        {
            Cache.Remove(OutputCachePartition, key);
        }
    }
}
