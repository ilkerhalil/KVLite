![](http://pomma89.altervista.org/kvlite/logo-64.png "KVLite Logo") KVLite
===============================================================================================================

*KVLite is a partition-based key-value cache built for SQL.*

KVLite can be stored either in persistent or volatile fashion, and each key/value pair can have its own lifetime and refresh mode.

## Summary ##

* Latest release version: `v6.2.3`
* Build status on [AppVeyor](https://ci.appveyor.com): [![Build status](https://ci.appveyor.com/api/projects/status/7qgv5o7or96rr8a2?svg=true)](https://ci.appveyor.com/project/pomma89/kvlite)
* [Doxygen](http://www.stack.nl/~dimitri/doxygen/index.html) documentation: TODO
* [NuGet](https://www.nuget.org) package(s):
    + [PommaLabs.KVLite](https://www.nuget.org/packages/PommaLabs.KVLite/), includes Core and all native libraries.
    + [PommaLabs.KVLite (Core)](https://www.nuget.org/packages/PommaLabs.KVLite.Core/), all managed APIs.
    + [PommaLabs.KVLite (Entity Framework Query Cache Provider)](https://www.nuget.org/packages/PommaLabs.KVLite.EntityFramework/)
    + [PommaLabs.KVLite (Nancy Caching Bootstrapper)](https://www.nuget.org/packages/PommaLabs.KVLite.Nancy/)
    + [PommaLabs.KVLite (Web API Output Cache Provider)](https://www.nuget.org/packages/PommaLabs.KVLite.WebApi/)
    + [PommaLabs.KVLite (Web Forms Caching Components)](https://www.nuget.org/packages/PommaLabs.KVLite.WebForms/)

## Introduction ##

Let's start with a simple example of what you can do with KVLite:

```cs
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
        persistentCache.AddTimed(examplePartition1, exampleKey1, simpleValue, persistentCache.Clock.UtcNow.AddMinutes(5));
        persistentCache.AddTimed(examplePartition1, exampleKey2, simpleValue, persistentCache.Clock.UtcNow.AddMinutes(10));
        persistentCache.AddTimed(examplePartition2, exampleKey1, complexValue, persistentCache.Clock.UtcNow.AddMinutes(10));
        persistentCache.AddTimed(examplePartition2, exampleKey2, complexValue, persistentCache.Clock.UtcNow.AddMinutes(5));
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
            InsertionCountBeforeAutoClean = 10, // Number of inserts before a cache cleanup is issued.
            MaxCacheSizeInMB = 64, // Max size in megabytes for the cache.
            MaxJournalSizeInMB = 16, // Max size in megabytes for the SQLite journal log.
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
        volatileCache.AddTimed(examplePartition1, exampleKey1, simpleValue, volatileCache.Clock.UtcNow.AddMinutes(10));
        volatileCache.AddTimed(examplePartition1, exampleKey2, complexValue, TimeSpan.FromMinutes(15));
        volatileCache.AddStatic(examplePartition2, exampleKey1, simpleValue);
        volatileCache.AddSliding(examplePartition2, exampleKey2, complexValue, TimeSpan.FromMinutes(15));
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
```

## About this repository and its maintainer ##

Everything done on this repository is freely offered on the terms of the project license. You are free to do everything you want with the code and its related files, as long as you respect the license and use common sense while doing it :-)

I maintain this project during my spare time, so I can offer limited assistance and I can offer **no kind of warranty**.

Development of this project is sponsored by [Finsa SpA](https://www.finsa.it), my current employer.
