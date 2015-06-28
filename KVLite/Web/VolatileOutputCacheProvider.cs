// File name: OutputCacheProvider.cs
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

namespace PommaLabs.KVLite.Web
{
    /// <summary>
    ///   A custom output cache provider based on KVLite. Specifically, it uses the instance defined
    ///   by the property <see cref="VolatileCache.DefaultInstance"/>.
    /// </summary>
    public sealed class VolatileOutputCacheProvider : System.Web.Caching.OutputCacheProvider
    {
        /// <summary>
        ///   The partition used by output cache provider items.
        /// </summary>
        private const string OutputCachePartition = "KVLite.Web.VolatileOutputCache";

        /// <summary>
        ///   Returns a reference to the specified entry in the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for a cached entry in the output cache.</param>
        /// <returns>
        ///   The <paramref name="key"/> value that identifies the specified entry in the cache, or
        ///   null if the specified entry is not in the cache.
        /// </returns>
        public override object Get(string key)
        {
            var item = VolatileCache.DefaultInstance.Get<object>(OutputCachePartition, key);
            return item.HasValue ? item.Value : null;
        }

        /// <summary>
        ///   Inserts the specified entry into the output cache.
        /// </summary>
        /// <param name="key">A unique identifier for <paramref name="entry"/>.</param>
        /// <param name="entry">The content to add to the output cache.</param>
        /// <param name="utcExpiry">The time and date on which the cached entry expires.</param>
        /// <returns>A reference to the specified provider.</returns>
        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            VolatileCache.DefaultInstance.AddTimed(OutputCachePartition, key, entry, utcExpiry);
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
            VolatileCache.DefaultInstance.AddTimed(OutputCachePartition, key, entry, utcExpiry);
        }

        /// <summary>
        ///   Removes the specified entry from the output cache.
        /// </summary>
        /// <param name="key">
        ///   The unique identifier for the entry to remove from the output cache.
        /// </param>
        public override void Remove(string key)
        {
            VolatileCache.DefaultInstance.Remove(OutputCachePartition, key);
        }
    }
}