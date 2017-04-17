// File name: Program.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using NodaTime;
using PommaLabs.KVLite.SQLite;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PommaLabs.KVLite.Examples
{
    /// <summary>
    ///   Learn how to use KVLite by examples.
    /// </summary>
    internal static class Program
    {
        /// <summary>
        ///   Learn how to use KVLite by examples.
        /// </summary>
        public static void Main()
        {
            // Some variables used in the examples.
            var examplePartition1 = "example partition 1";
            var examplePartition2 = "example partition 2";
            var exampleKey1 = "example key 1";
            var exampleKey2 = "example key 2";
            var simpleValue = Math.PI;
            var complexValue = new ComplexValue
            {
                Integer = 21,
                NullableBoolean = null,
                String = "Learning KVLite",
                Dictionary = new Dictionary<short, ComplexValue>
                {
                    [1] = new ComplexValue { NullableBoolean = true },
                    [2] = new ComplexValue { String = "Nested..." }
                }
            };

            /*
             * KVLite stores its values inside a given partition and each value is linked to a key.
             * KVLite can contain more than one partition and each partition can contain more than one key.
             *
             * Therefore, values are stored according to this logical layout:
             *
             * [partition1] --> key1/value1
             *              --> key2/value2
             * [partition2] --> key1/value1
             *              --> key2/value2
             *              --> key3/value3
             *
             * A key is unique inside a partition, not inside all cache.
             * A partition, instead, is unique inside all cache.
             */

            // You can start using the default caches immediately. Let's try to store some values in
            // a way similar to the figure above, using the default persistent cache.
            ICache persistentCache = PersistentCache.DefaultInstance;
            persistentCache.AddTimed(examplePartition1, exampleKey1, simpleValue, persistentCache.Clock.GetCurrentInstant() + Duration.FromMinutes(5));
            persistentCache.AddTimed(examplePartition1, exampleKey2, simpleValue, persistentCache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            persistentCache.AddTimed(examplePartition2, exampleKey1, complexValue, persistentCache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            persistentCache.AddTimed(examplePartition2, exampleKey2, complexValue, persistentCache.Clock.GetCurrentInstant() + Duration.FromMinutes(5));
            PrettyPrint(persistentCache);

            // Otherwise, you can customize you own cache... Let's see how we can use a volatile
            // cache. Let's define the settings that we will use in new volatile caches.
            var volatileCacheSettings = new VolatileCacheSettings
            {
                CacheName = "My In-Memory Cache", // The backend.
                StaticIntervalInDays = 10 // How many days static values will last.
            };

            // Then the settings that we will use in new persistent caches.
            var persistentCacheSettings = new PersistentCacheSettings
            {
                CacheFile = "CustomCache.sqlite", // The SQLite DB used as the backend for the cache.
                ChancesOfAutoCleanup = 10, // Number of inserts before a cache cleanup is issued.
                StaticIntervalInDays = 10 // How many days static values will last.
            };

            // We create both a volatile and a persistent cache.
            var volatileCache = new VolatileCache(volatileCacheSettings);
            persistentCache = new PersistentCache(persistentCacheSettings);

            // Use the new volatile cache!
            volatileCache.AddStatic(examplePartition1, exampleKey1, Tuple.Create("Volatile!", 123));
            PrettyPrint(volatileCache);

            // Use the new persistent cache!
            persistentCache.AddStatic(examplePartition2, exampleKey2, Tuple.Create("Persistent!", 123));
            PrettyPrint(persistentCache);

            /*
             * An item can be added to the cache in three different ways.
             *
             * "Timed" values last until the specified date and time, or for a specified timespan.
             * Reading them will not extend their lifetime.
             *
             * "Sliding" values last for the specified lifetime, but, if read,
             * their lifetime will be extended by the timespan specified initially.
             *
             * "Static" values are a special form of "sliding" values.
             * They use a very long timespan, 30 days by default, and they can be used for seldom changed data.
             */

            // Let's clear the volatile cache and let's a value for each type.
            volatileCache.Clear();
            volatileCache.AddTimed(examplePartition1, exampleKey1, simpleValue, volatileCache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            volatileCache.AddTimed(examplePartition1, exampleKey2, complexValue, Duration.FromMinutes(15));
            volatileCache.AddStatic(examplePartition2, exampleKey1, simpleValue);
            volatileCache.AddSliding(examplePartition2, exampleKey2, complexValue, Duration.FromMinutes(15));
            PrettyPrint(volatileCache);

            Console.Read();
        }

        private static void PrettyPrint(ICache cache)
        {
            Console.WriteLine($"Printing the contents of a {cache.GetType().Name}");

            // When we use "Peek*" methods, the expiration time of items is left untouched.
            var cacheItems = cache.PeekItems<object>();
            foreach (var cacheItem in cacheItems.OrderBy(ci => ci.Partition).ThenBy(ci => ci.Key))
            {
                Console.WriteLine($"{cacheItem.Partition} --> {cacheItem.Key} --> {cacheItem.Value}");
            }

            Console.WriteLine();
        }

        private sealed class ComplexValue
        {
            public int Integer { get; set; }
            public bool? NullableBoolean { get; set; }
            public string String { get; set; }
            public IDictionary<short, ComplexValue> Dictionary { get; set; }

            public override string ToString() => nameof(ComplexValue);
        }
    }
}
