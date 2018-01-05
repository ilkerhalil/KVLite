# ![](http://pomma89.altervista.org/kvlite/logo-64.png "KVLite Logo") KVLite

*KVLite is a partition-based key-value cache built for SQL RDBMSs.*

[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=ELJWKEYS9QGKA)

KVLite entries can be stored either in persistent or volatile fashion, 
and each key/value pair can have its own lifetime and refresh mode.

Entries are grouped by partition, where each key must be unique only inside the partition it belongs to.

Following RDBMSs are currently supported by KVLite or will be supported soon:

* MySQL and MariaDB (.NET and .NET Core)
* Oracle (.NET only, expected driver in Q1/2018)
* PostgreSQL (.NET and .NET Core)
* SQL Server (.NET and .NET Core)
* SQLite (.NET and .NET Core)

KVLite implements different caching adapters for various libraries and frameworks:

* ASP.NET Core distributed cache and session.
* ASP.NET WebAPI output cache.
* ASP.NET WebForms output cache and viewstate storage.
* Entity Framework query cache.

## Summary

* Latest release version: `v7.0.1`
* Build status on [Travis CI](https://travis-ci.org): [![Build Status](https://travis-ci.org/pomma89/KVLite.svg?branch=master)](https://travis-ci.org/pomma89/KVLite)
* Build status on [AppVeyor](https://www.appveyor.com/): [![Build status](https://ci.appveyor.com/api/projects/status/n0tbcdkbx3vs8nmk/branch/master?svg=true)](https://ci.appveyor.com/project/pomma89/kvlite/branch/master)
* [Wyam](https://wyam.io/) generated API documentation: [https://pomma89.github.io/KVLite/api/](https://pomma89.github.io/KVLite/api/)
* [NuGet](https://www.nuget.org) package(s):
    + [PommaLabs.KVLite](https://www.nuget.org/packages/PommaLabs.KVLite/), caching interfaces and base classes.
    + [PommaLabs.KVLite.Core](https://www.nuget.org/packages/PommaLabs.KVLite.Core/), core implementations.
    + [PommaLabs.KVLite.Memory](https://www.nuget.org/packages/PommaLabs.KVLite.Memory/), default in-memory driver.
    + [PommaLabs.KVLite.Database](https://www.nuget.org/packages/PommaLabs.KVLite.Database/), SQL code shared by all drivers.
    + [PommaLabs.KVLite.MySql](https://www.nuget.org/packages/PommaLabs.KVLite.MySql/), driver for MySQL and MariaDB.
    + [PommaLabs.KVLite.Oracle](https://www.nuget.org/packages/PommaLabs.KVLite.Oracle/), driver for Oracle.
    + [PommaLabs.KVLite.PostgreSql](https://www.nuget.org/packages/PommaLabs.KVLite.PostgreSql/), driver for PostgreSQL.
    + [PommaLabs.KVLite.SqlServer](https://www.nuget.org/packages/PommaLabs.KVLite.SqlServer/), driver for SQL Server.
    + [PommaLabs.KVLite.SQLite](https://www.nuget.org/packages/PommaLabs.KVLite.SQLite/), driver for SQLite.
    + [PommaLabs.KVLite.AspNetCore](https://www.nuget.org/packages/PommaLabs.KVLite.AspNetCore/), distributed caching and session helpers.
    + [PommaLabs.KVLite.EntityFramework](https://www.nuget.org/packages/PommaLabs.KVLite.EntityFramework/), query cache provider.
    + [PommaLabs.KVLite.WebApi](https://www.nuget.org/packages/PommaLabs.KVLite.WebApi/), output cache provider.
    + [PommaLabs.KVLite.WebForms](https://www.nuget.org/packages/PommaLabs.KVLite.WebForms/), caching components.

### How to build

#### Windows

Clone the project, go to the root and run PowerShell script `build.ps1`. In order for it to work, you need:

* At least Windows 10 Fall Creators Update
* At least Visual Studio 2017 Update 4
* .NET Framework 4.7.1 Developer Pack
* .NET Core 2.0 SDK

#### Linux

Clone the project, go to the root and run Bash script `build.sh`. In order for it to work, you need:

* TODO, still need to make it building reliably.

## Introduction

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
            StaticInterval = Duration.FromDays(10) // How long static values will last.
        };

        // Then the settings that we will use in new persistent caches.
        var persistentCacheSettings = new PersistentCacheSettings
        {
            CacheFile = "CustomCache.sqlite", // The SQLite DB used as the backend for the cache.
            ChancesOfAutoCleanup = 0.5, // Chance of an automatic a cache cleanup being issued.
            StaticInterval = Duration.FromDays(10) // How long static values will last.
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
```

### Examples

Further examples can be found in the following projects:

* [ASP.NET Core](https://github.com/pomma89/KVLite/tree/master/examples/PommaLabs.KVLite.Examples.AspNetCore): It shows how to register KVLite services and how to use it as a proper distributed cache implementation. Moreover, it shows how to use KVLite session extensions.
* [ASP.NET WebAPI](https://github.com/pomma89/KVLite/tree/master/examples/PommaLabs.KVLite.Examples.WebApi): You can find how to cache action results using KVLite adapter.
* [ASP.NET WebForms](https://github.com/pomma89/KVLite/tree/master/examples/PommaLabs.KVLite.Examples.WebForms): It contains a simple page configured to use KVLite as a custom viewstate persister.

## Storage layout

KVLite stores cache entries in a dedicated table, whose schema is as much tuned as possible for each RDBMS.
The logical schema for cache entries table is the following:

| Column name         | Data type        | Content                                                                            |
|---------------------|------------------|------------------------------------------------------------------------------------|
| `kvle_id`           | `guid` or `long` | Automatically generated ID. This is the primary key.                               |
| `kvle_hash`         | `long`           | Hash of partition and key. This is the unique key.                                 |
| `kvle_expiry`       | `long`           | When the entry will expire, expressed as seconds after UNIX epoch.                 |
| `kvle_interval`     | `long`           | How many seconds should be used to extend expiry time when the entry is retrieved. |
| `kvle_value`        | `byte[]`         | Serialized and optionally compressed content of this entry.                        |
| `kvle_compressed`   | `bool`           | Whether the entry content was compressed or not.                                   |
| `kvle_partition`    | `string`         | A partition holds a group of related keys.                                         |
| `kvle_key`          | `string`         | A key uniquely identifies an entry inside a partition.                             |
| `kvle_creation`     | `long`           | When the entry was created, expressed as seconds after UNIX epoch.                 |
| `kvle_parent_hash0` | `long`           | Optional parent entry hash, used to link entries in a hierarchical way.            |
| `kvle_parent_key0`  | `string`         | Optional parent entry key, used to link entries in a hierarchical way.             |
| `kvle_parent_hash1` | `long`           | Optional parent entry hash, used to link entries in a hierarchical way.            |
| `kvle_parent_key1`  | `string`         | Optional parent entry key, used to link entries in a hierarchical way.             |
| `kvle_parent_hash2` | `long`           | Optional parent entry hash, used to link entries in a hierarchical way.            |
| `kvle_parent_key2`  | `string`         | Optional parent entry key, used to link entries in a hierarchical way.             |

Specialized schemas for supported RDBMS systems are available inside this project repository or at following links:

* [MySQL and MariaDB](https://github.com/pomma89/KVLite/blob/master/src/PommaLabs.KVLite.MySql/Scripts/kvl_cache_entries.sql)
* [Oracle](https://github.com/pomma89/KVLite/blob/master/src/PommaLabs.KVLite.Oracle/Scripts/kvl_cache_entries.sql)
* [PostgreSQL](https://github.com/pomma89/KVLite/blob/master/src/PommaLabs.KVLite.PostgreSql/Scripts/kvl_cache_entries.sql)
* [SQL Server](https://github.com/pomma89/KVLite/blob/master/src/PommaLabs.KVLite.SqlServer/Scripts/kvl_cache_entries.sql)

Each script might have a few comments suggesting how to further optimize cache entries table storage depending on the actual version of the specific RDBMS system.

### Customizing table name or SQL schema

Default name for cache entries table is `kvle_cache_entries` and default SQL schema is `kvlite`. 
However, those values can be easily changed at runtime, as we do in the following snippet:

```cs
// Change cache entries table name for Oracle cache.
OracleCache.DefaultInstance.Settings.CacheEntriesTableName = "my_custom_name";

// Change SQL schema name for MySQL cache.
MySqlCache.DefaultInstance.Settings.CacheSchemaName = "my_schema_name";

// Change both table ans schema name for SQL Server cache.
SqlServerCache.DefaultInstance.Settings.CacheEntriesTableName = "my_custom_name";
SqlServerCache.DefaultInstance.Settings.CacheSchemaName = "my_schema_name";
```

Please perform those customizations as early as your application starts; for example, these are good places where to put the lines:

* `Program.cs` for console and Windows applications.
* `Global.asax.cs` for classic web applications.
* `Startup.cs` for Owin-based web applications.

## About this repository and its maintainer

Everything done on this repository is freely offered on the terms of the project license. You are free to do everything you want with the code and its related files, as long as you respect the license and use common sense while doing it :-)

I maintain this project during my spare time, so I can offer limited assistance and I can offer **no kind of warranty**.

However, if this project helps you, then you might offer me an hot cup of coffee:

[![Donate](http://pomma89.altervista.org/buy-me-a-coffee.png)](https://www.paypal.com/cgi-bin/webscr?cmd=_s-xclick&hosted_button_id=ELJWKEYS9QGKA)
