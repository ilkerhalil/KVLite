// File name: Program.cs
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

using System;
using PommaLabs.KVLite;

namespace Examples
{
    internal static class Program
    {
        public static void Main(string[] args)
        {
            // You can start using the default caches immediately! Configure them through the KVLite configuration file.
            const string partition = "examples";
            const string key = "hello!";
            PersistentCache.DefaultInstance.AddTimed(partition, key, 123, DateTime.UtcNow.AddMinutes(5));
            Console.WriteLine("My persistent value is: " + PersistentCache.DefaultInstance[partition, key].Value);

            // Otherwise, you can customize you own cache...

            // Settings that we will use in new volatile caches.
            var volatileCacheSettings = new VolatileCacheSettings
            {
                CacheName = "My In-Memory Cache", // The backend.
                StaticIntervalInDays = 10 // How many days static values will last.
            };

            // Settings that we will use in new persistent caches.
            var persistentCacheSettings = new PersistentCacheSettings
            {
                CacheFile = "PersistentCache.sqlite", // The SQLite DB used as the backend for the cache.
                InsertionCountBeforeAutoClean = 10, // Number of inserts before a cache cleanup is issued.
                MaxCacheSizeInMB = 64, // Max size in megabytes for the cache.
                MaxJournalSizeInMB = 16, // Max size in megabytes for the SQLite journal log.
                StaticIntervalInDays = 10 // How many days static values will last.
            };

            // We create both a volatile and a persistent cache.
            var volatileCache = new VolatileCache(volatileCacheSettings);
            var persistentCache = new PersistentCache(persistentCacheSettings);
            
            // Use the new volatile cache!
            volatileCache.AddStatic(partition, key, Tuple.Create("Volatile!", 123));
            Console.WriteLine("My volatile value is: " + volatileCache[partition, key].Value);

            Console.Read();
        }
    }
}