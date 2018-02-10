// File name: Program.cs
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

using BenchmarkDotNet.Running;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.Oracle;
using PommaLabs.KVLite.PostgreSql;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.SqlServer;
using PommaLabs.KVLite.UnitTests;
using Serilog;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Benchmarks
{
    public static class Program
    {
        private const int IterationCount = 3;
        private const int TablesCount = 1000;
        private const int RowCount = 100;
        private const int LogMessagesCount = 3000;
        private const string Spacer = "------------------------------";

        private static readonly string[] ColumnNames = { "A", "B", "C", "D", "E" };

        private static IList<DataTable> _tables;
        private static double _tableListSize;
        private static IList<LogMessage> _logMessages;
        private static double _logMessagesSize;

        public static async Task Main(string[] args)
        {
            // Configure Serilog and LibLog logging.
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .Enrich.WithDemystifiedStackTraces()
                .CreateLogger();

            // Retrieve additional connection strings from command line arguments.
            args = ConnectionStrings.ReadFromCommandLineArguments(args);

            if (args.Length >= 1)
            {
                BenchmarkSwitcher.FromTypes(new[]
                {
                    typeof(Comparison)
                }).Run(args);
                return;
            }

            Log.Information("Configuring SQL caches...");
            MySqlCache.DefaultInstance.Settings.ConnectionString = ConnectionStrings.MySql;
            PostgreSqlCache.DefaultInstance.Settings.ConnectionString = ConnectionStrings.PostgreSql;
            SqlServerCache.DefaultInstance.Settings.ConnectionString = ConnectionStrings.SqlServer;

            OracleCache.DefaultInstance.Settings.CacheSchemaName = "CARAVAN";
            OracleCache.DefaultInstance.Settings.CacheEntriesTableName = "CRVN_KVL_ENTRIES";
            OracleCache.DefaultInstance.Settings.ConnectionString = ConnectionStrings.Oracle;

            PersistentCache.DefaultInstance.Settings.CacheFile = Path.GetTempFileName();

            Log.Information("Running vacuum on SQLite persistent cache...");
            PersistentCache.DefaultInstance.Vacuum();

            Log.Information("Generating random data tables...");
            _tables = GenerateRandomDataTables();
            _tableListSize = GetObjectSizeInMB(_tables);

            Log.Information($"Table count: {TablesCount}");
            Log.Information($"Row count: {RowCount}");
            Log.Information($"Total table size: {_tableListSize:0.0} MB");

            Log.Information("Generating random log messages...");
            _logMessages = LogMessage.GenerateRandomLogMessages(LogMessagesCount);
            _logMessagesSize = GetObjectSizeInMB(_logMessages);

            Log.Information($"Log messages count: {TablesCount}");
            Log.Information($"Total log messages size: {_logMessagesSize:0.0} MB");

            var caches = new ICache[]
            {
                MemoryCache.DefaultInstance,
                MySqlCache.DefaultInstance,
                //OracleCache.DefaultInstance,
                PostgreSqlCache.DefaultInstance,
                SqlServerCache.DefaultInstance,
                PersistentCache.DefaultInstance,
                VolatileCache.DefaultInstance
            };

            var asyncCaches = caches.Cast<IAsyncCache>().ToArray();

            for (var i = 0; i < IterationCount; ++i)
            {
                /*** STORE EACH LOG MESSAGE ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await StoreEachLogMessageAsync(asyncCache, i);
                }

                /*** STORE EACH DATA TABLE ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await StoreEachDataTableAsync(asyncCache, i);
                }

                /*** STORE AND RETRIEVE EACH DATA TABLE TWO TIMES ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await StoreAndRetrieveEachDataTableAsync(asyncCache, i);
                }

                /*** STORE EACH DATA TABLE ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    StoreEachDataTable(cache, i);
                }

                /*** STORE EACH DATA TABLE TWO TIMES ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    StoreEachDataTableTwoTimes(cache, i);
                }

                /*** REMOVE EACH DATA TABLE ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    RemoveEachDataTable(cache, i);
                }

                /*** REMOVE EACH DATA TABLE ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await RemoveEachDataTableAsync(asyncCache, i);
                }

                /*** PEEK EACH DATA TABLE ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    PeekEachDataTable(cache, i);
                }

                /*** RETRIEVE EACH DATA TABLE ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    RetrieveEachDataTable(cache, i);
                }

                /*** RETRIEVE EACH DATA TABLE ITEM ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    RetrieveEachDataTableItem(cache, i);
                }

                /*** RETRIEVE EACH DATA TABLE ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await RetrieveEachDataTableAsync(asyncCache, i);
                }

                /*** STORE DATA TABLES LIST ***/

                FullyCleanCaches();
                foreach (var cache in caches)
                {
                    StoreDataTableList(cache, i);
                }

                /*** STORE EACH DATA TABLE TWO TIMES ASYNC ***/

                await FullyCleanCachesAsync();
                foreach (var asyncCache in asyncCaches)
                {
                    await StoreEachDataTableTwoTimesAsync(asyncCache, i);
                }
            }

            FullyCleanCaches();

            Console.WriteLine();
            Console.Write("Press any key to exit...");
            Console.Read();
        }

        private static IList<DataTable> GenerateRandomDataTables()
        {
            var gen = new RandomDataTableGenerator(ColumnNames);
            var list = new List<DataTable>();
            for (var i = 0; i < TablesCount; ++i)
            {
                list.Add(gen.GenerateDataTable(RowCount));
            }
            return list;
        }

        private static void FullyCleanCaches()
        {
            Log.Warning("Fully cleaning all caches...");
            MemoryCache.DefaultInstance.Clear();
            MySqlCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            //OracleCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            PostgreSqlCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            SqlServerCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            VolatileCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
        }

        private static async Task FullyCleanCachesAsync()
        {
            Log.Warning("Fully cleaning all caches (async)...");
            await MemoryCache.DefaultInstance.ClearAsync();
            await MySqlCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
            //await OracleCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
            await PostgreSqlCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
            await SqlServerCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
            await PersistentCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
            await VolatileCache.DefaultInstance.ClearAsync(CacheReadMode.IgnoreExpiryDate);
        }

        private static void StoreDataTableList<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing data table list, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            cache.AddStatic("TABLE_LIST", _tables);
            stopwatch.Stop();

            Debug.Assert(cache.Count() == 1);
            Debug.Assert(cache.LongCount() == 1);

            Log.Information($"[{cacheName}] Data table list stored in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTable<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing each data table, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == _tables.Count);
            Debug.Assert(cache.LongCount() == _tables.LongCount());

            Log.Information($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableTwoTimes<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing each data table two times, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == _tables.Count);
            Debug.Assert(cache.LongCount() == _tables.LongCount());

            Log.Information($"[{cacheName}] Data tables stored two times in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreEachLogMessageAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing each log message asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _logMessages.ParallelForEachAsync(async (logMessage, i) =>
            {
                var logMessageKey = i.ToString();
                await cache.AddStaticAsync(logMessageKey, logMessage);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == _logMessages.Count);
            Debug.Assert(await cache.LongCountAsync() == _logMessages.LongCount());

            Log.Information($"[{cacheName}] Log messages stored in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_logMessagesSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreEachDataTableAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == _tables.Count);
            Debug.Assert(await cache.LongCountAsync() == _tables.LongCount());

            Log.Information($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreEachDataTableTwoTimesAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing each data table (two times, asynchronously), iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == _tables.Count);
            Debug.Assert(await cache.LongCountAsync() == _tables.LongCount());

            Log.Information($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024L * 1024L)} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize * 2 / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RetrieveEachDataTable<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Retrieving each data table, iteration {iteration}...");

            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                var returnedTable = cache.Get<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Log.Information($"[{cacheName}] Data tables retrieved in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task RetrieveEachDataTableAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Retrieving each data table asynchronously, iteration {iteration}...");

            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _tables.ParallelForEachAsync(async table =>
            {
                var returnedTable = await cache.GetAsync<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Log.Information($"[{cacheName}] Data tables retrieved in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024L * 1024L)} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RetrieveEachDataTableItem<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Retrieving each data table item, iteration {iteration}...");

            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                var returnedTable = cache.GetItem<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table item read from cache! :(");
                }
            }
            stopwatch.Stop();

            Log.Information($"[{cacheName}] Data table items retrieved in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void PeekEachDataTable<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            if (!cache.CanPeek)
            {
                return;
            }

            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Peeking each data table, iteration {iteration}...");

            foreach (var table in _tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                var returnedTable = cache.Peek<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Log.Information($"[{cacheName}] Data tables peeked in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RemoveEachDataTable<TCache>(TCache cache, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Removing each data table, iteration {iteration}...");

            foreach (var table in _tables)
            {
                cache.AddTimed(table.TableName, table, cache.Clock.UtcNow + TimeSpan.FromHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in _tables)
            {
                cache.Remove(table.TableName);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == 0);
            Debug.Assert(cache.LongCount() == 0L);

            Log.Information($"[{cacheName}] Data tables removed in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task RemoveEachDataTableAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Removing each data table asynchronously, iteration {iteration}...");

            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddTimedAsync(table.TableName, table, cache.Clock.UtcNow + TimeSpan.FromHours(1));
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.RemoveAsync(table.TableName);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == 0);
            Debug.Assert(await cache.LongCountAsync() == 0L);

            Log.Information($"[{cacheName}] Data tables removed in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024.0 * 1024.0):0.0} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreAndRetrieveEachDataTableAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(Spacer);
            Log.Information($"[{cacheName}] Storing and retrieving each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
                var returnedTable = await cache.GetAsync<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == _tables.Count);
            Debug.Assert(await cache.LongCountAsync() == _tables.LongCount());

            Log.Information($"[{cacheName}] Data tables stored and retrieved in: {stopwatch.Elapsed}");
            Log.Information($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024L * 1024L)} MB");
            Log.Information($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize * 2 / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static double GetObjectSizeInMB(object obj)
        {
            double result;
            using (var ms = new PooledMemoryStream())
            {
                new BinarySerializer().SerializeToStream(obj, ms);
                result = ms.Length / (1024.0 * 1024.0);
            }
            GC.Collect();
            return result;
        }
    }
}
