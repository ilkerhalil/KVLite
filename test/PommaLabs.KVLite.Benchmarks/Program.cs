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

using BenchmarkDotNet.Running;
using NodaTime;
using PommaLabs.KVLite.Benchmarks.Compression;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Benchmarks.Serialization;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.Oracle;
using PommaLabs.KVLite.PostgreSql;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.SqlServer;
using PommaLabs.KVLite.UnitTests;
using System;
using System.Collections.Async;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Benchmarks
{
    public static class Program
    {
        private const int RowCount = 100;
        private const int IterationCount = 5;
        private const int RandomItemCount = 1000;
        private const int LogMessagesCount = 1000;

        private static readonly string[] ColumnNames = { "A", "B", "C", "D", "E" };

        private static double _tableListSize;

        private static LogMessage[] _logMessages;
        private static double _logMessagesSize;

        public static async Task Main(string[] args)
        {
            if (args.Length >= 1)
            {
                BenchmarkSwitcher.FromTypes(new[]
                {
                    typeof(Comparison),
                    typeof(LogMessagesCompression),
                    typeof(LogMessagesSerialization)
                }).Run(args);
                return;
            }

            MySqlCache.DefaultInstance.Settings.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(MySql)].ConnectionString;
            PostgreSqlCache.DefaultInstance.Settings.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(PostgreSql)].ConnectionString;
            SqlServerCache.DefaultInstance.Settings.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(SqlServer)].ConnectionString;

            OracleCache.DefaultInstance.Settings.CacheSchemaName = "CARAVAN";
            OracleCache.DefaultInstance.Settings.CacheEntriesTableName = "CRVN_KVL_ENTRIES";
            OracleCache.DefaultInstance.Settings.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(Oracle)].ConnectionString;

            PersistentCache.DefaultInstance.Settings.CacheFile = Path.GetTempFileName();

            Console.WriteLine(@"Running vacuum on DB...");
            PersistentCache.DefaultInstance.Vacuum();
            Console.WriteLine(@"Vacuum completed.");

            Console.WriteLine();
            Console.WriteLine(@"Generating random data tables...");
            var tables = GenerateRandomDataTables();
            _tableListSize = GetObjectSizeInMB(tables);
            _logMessages = LogMessage.GenerateRandomLogMessages(LogMessagesCount);
            _logMessagesSize = GetObjectSizeInMB(_logMessages);
            GC.Collect();
            Console.WriteLine(@"Tables generated!");
            Console.WriteLine($@"Table count: {RandomItemCount}");
            Console.WriteLine($@"Row count: {RowCount}");
            Console.WriteLine($@"Total table size: {_tableListSize:0.0} MB");
            Console.WriteLine($@"Total log messages size: {_logMessagesSize:0.0} MB");

            for (var i = 0; i < IterationCount; ++i)
            {
                /*** STORE EACH LOG MESSAGE ASYNC ***/

                FullyCleanCaches();
                await StoreEachLogMessageAsync(MySqlCache.DefaultInstance, i);
                //await StoreEachLogMessageAsync(OracleCache.DefaultInstance, i);
                await StoreEachLogMessageAsync(PostgreSqlCache.DefaultInstance, i);
                await StoreEachLogMessageAsync(SqlServerCache.DefaultInstance, i);

                FullyCleanCaches();
                await StoreEachLogMessageAsync(PersistentCache.DefaultInstance, i);

                FullyCleanCaches();
                await StoreEachLogMessageAsync(VolatileCache.DefaultInstance, i);

                FullyCleanCaches();
                await StoreEachLogMessageAsync(MemoryCache.DefaultInstance, i);

                /*** STORE EACH DATA TABLE ASYNC ***/

                FullyCleanCaches();
                await StoreEachDataTableAsync(MySqlCache.DefaultInstance, tables, i);
                //await StoreEachDataTableAsync(OracleCache.DefaultInstance, tables, i);
                await StoreEachDataTableAsync(PostgreSqlCache.DefaultInstance, tables, i);
                await StoreEachDataTableAsync(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                await StoreEachDataTableAsync(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                await StoreEachDataTableAsync(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                await StoreEachDataTableAsync(MemoryCache.DefaultInstance, tables, i);

                /*** STORE EACH DATA TABLE ***/

                FullyCleanCaches();
                StoreEachDataTable(MySqlCache.DefaultInstance, tables, i);
                //StoreEachDataTable(OracleCache.DefaultInstance, tables, i);
                StoreEachDataTable(PostgreSqlCache.DefaultInstance, tables, i);
                StoreEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** STORE EACH DATA TABLE TWO TIMES ***/

                FullyCleanCaches();
                StoreEachDataTableTwoTimes(MySqlCache.DefaultInstance, tables, i);
                //StoreEachDataTableTwoTimes(OracleCache.DefaultInstance, tables, i);
                StoreEachDataTableTwoTimes(PostgreSqlCache.DefaultInstance, tables, i);
                StoreEachDataTableTwoTimes(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTableTwoTimes(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTableTwoTimes(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                StoreEachDataTableTwoTimes(MemoryCache.DefaultInstance, tables, i);

                /*** REMOVE EACH DATA TABLE ***/

                FullyCleanCaches();
                RemoveEachDataTable(MySqlCache.DefaultInstance, tables, i);
                //RemoveEachDataTable(OracleCache.DefaultInstance, tables, i);
                RemoveEachDataTable(PostgreSqlCache.DefaultInstance, tables, i);
                RemoveEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** REMOVE EACH DATA TABLE ASYNC ***/

                FullyCleanCaches();
                RemoveEachDataTableAsync(MySqlCache.DefaultInstance, tables, i);
                //RemoveEachDataTableAsync(OracleCache.DefaultInstance, tables, i);
                RemoveEachDataTableAsync(PostgreSqlCache.DefaultInstance, tables, i);
                RemoveEachDataTableAsync(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTableAsync(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTableAsync(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RemoveEachDataTableAsync(MemoryCache.DefaultInstance, tables, i);

                /*** PEEK EACH DATA TABLE ***/

                FullyCleanCaches();
                PeekEachDataTable(MySqlCache.DefaultInstance, tables, i);
                //PeekEachDataTable(OracleCache.DefaultInstance, tables, i);
                PeekEachDataTable(PostgreSqlCache.DefaultInstance, tables, i);
                PeekEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                PeekEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                PeekEachDataTable(VolatileCache.DefaultInstance, tables, i);

                //FullyCleanCache(); < --Memory cache does not allow peeking!
                //PeekEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** RETRIEVE EACH DATA TABLE ***/

                FullyCleanCaches();
                RetrieveEachDataTable(MySqlCache.DefaultInstance, tables, i);
                //RetrieveEachDataTable(OracleCache.DefaultInstance, tables, i);
                RetrieveEachDataTable(PostgreSqlCache.DefaultInstance, tables, i);
                RetrieveEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** RETRIEVE EACH DATA TABLE ITEM ***/

                FullyCleanCaches();
                RetrieveEachDataTableItem(MySqlCache.DefaultInstance, tables, i);
                //RetrieveEachDataTableItem(OracleCache.DefaultInstance, tables, i);
                RetrieveEachDataTableItem(PostgreSqlCache.DefaultInstance, tables, i);
                RetrieveEachDataTableItem(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTableItem(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTableItem(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCaches();
                RetrieveEachDataTableItem(MemoryCache.DefaultInstance, tables, i);

                /*** TODO ***/

                FullyCleanCaches();
                StoreEachDataTableAsync_Volatile(tables, i);

                FullyCleanCaches();
                StoreEachDataTableAsync_Memory(tables, i);

                FullyCleanCaches();
                RetrieveEachDataTableAsync(tables, i);

                FullyCleanCaches();
                StoreEachDataTableTwoTimesAsync(tables, i);

                FullyCleanCaches();
                StoreAndRetrieveEachDataTableAsync(tables, i);

                FullyCleanCaches();
                StoreAndRetrieveEachDataTableAsync_Volatile(tables, i);

                FullyCleanCaches();
                StoreDataTableList(tables, i);
            }

            FullyCleanCaches();

            Console.WriteLine();
            Console.Write(@"Press any key to exit...");
            Console.Read();
        }

        private static IList<DataTable> GenerateRandomDataTables()
        {
            var gen = new RandomDataTableGenerator(ColumnNames);
            var list = new List<DataTable>();
            for (var i = 0; i < RandomItemCount; ++i)
            {
                list.Add(gen.GenerateDataTable(RowCount));
            }
            return list;
        }

        private static void FullyCleanCaches()
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Fully cleaning all caches...");
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            VolatileCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            MySqlCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            //OracleCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            PostgreSqlCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            SqlServerCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            MemoryCache.DefaultInstance.Clear();
            Console.WriteLine(@"All cache have been cleaned!");
        }

        private static void StoreDataTableList(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"Storing data table list, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            PersistentCache.DefaultInstance.AddStatic("TABLE_LIST", tables);
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 1);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 1);

            Console.WriteLine($@"Data table list stored in: {stopwatch.Elapsed}");
            Console.WriteLine($@"Current cache size: {PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == tables.Count);
            Debug.Assert(cache.LongCount() == tables.LongCount());

            Console.WriteLine($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableTwoTimes<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table two times, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == tables.Count);
            Debug.Assert(cache.LongCount() == tables.LongCount());

            Console.WriteLine($"[{cacheName}] Data tables stored two times in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreEachLogMessageAsync<TCache>(TCache cache, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each log message asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await _logMessages.ParallelForEachAsync(async (logMessage, i) =>
            {
                var logMessageKey = i.ToString();
                await cache.AddStaticAsync(logMessageKey, logMessage);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == _logMessages.Length);
            Debug.Assert(await cache.LongCountAsync() == _logMessages.LongLength);

            Console.WriteLine($"[{cacheName}] Log messages stored in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_logMessagesSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static async Task StoreEachDataTableAsync<TCache>(TCache cache, IList<DataTable> tables, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            await tables.ParallelForEachAsync(async table =>
            {
                await cache.AddStaticAsync(table.TableName, table);
            }, maxDegreeOfParalellism: Environment.ProcessorCount * 2);
            stopwatch.Stop();

            Debug.Assert(await cache.CountAsync() == tables.Count);
            Debug.Assert(await cache.LongCountAsync() == tables.LongCount());

            Console.WriteLine($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {await cache.GetCacheSizeInBytesAsync() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"[Volatile] Storing each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(Task.Run(() => VolatileCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(VolatileCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(VolatileCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine($@"[Volatile] Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($@"[Volatile] Current cache size: {VolatileCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"[Volatile] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableAsync_Memory(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"[Memory] Storing each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(Task.Run(() => MemoryCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(MemoryCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(MemoryCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine($@"[Memory] Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($@"[Memory] Current cache size: {MemoryCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"[Memory] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableTwoTimesAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"Storing each data table (two times, asynchronously), iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(Task.Run(() => PersistentCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            foreach (var table in tables)
            {
                tasks.Add(Task.Run(() => PersistentCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine($@"Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($@"Current cache size: {PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"Approximate speed (MB/sec): {_tableListSize * 2 / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RetrieveEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Retrieving each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.Get<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine($"[{cacheName}] Data tables retrieved in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RetrieveEachDataTableItem<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Retrieving each data table item, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.GetItem<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table item read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine($"[{cacheName}] Data table items retrieved in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void PeekEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Peeking each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.Peek<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine($"[{cacheName}] Data tables peeked in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RemoveEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Removing each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddTimed(table.TableName, table, cache.Clock.GetCurrentInstant() + Duration.FromHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.Remove(table.TableName);
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 0);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 0L);

            Console.WriteLine($"[{cacheName}] Data tables removed in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RemoveEachDataTableAsync<TCache>(TCache cache, IList<DataTable> tables, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = cache.Settings.CacheName;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Removing each data table asynchronously, iteration {iteration}...");

            var tasks = new Task[tables.Count];
            for (var i = 0; i < tables.Count; ++i)
            {
                var table = tables[i];
                tasks[i] = Task.Run(async () => await cache.AddTimedAsync(table.TableName, table, cache.Clock.GetCurrentInstant() + Duration.FromHours(1)));
            }
            Task.WaitAll(tasks);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < tables.Count; ++i)
            {
                var table = tables[i];
                tasks[i] = Task.Run(async () => await cache.RemoveAsync(table.TableName));
            }
            Task.WaitAll(tasks);
            stopwatch.Stop();

            Debug.Assert(cache.CountAsync().Result == 0);
            Debug.Assert(cache.LongCountAsync().Result == 0L);

            Console.WriteLine($"[{cacheName}] Data tables removed in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytesAsync().Result / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void RetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"Retrieving each data table asynchronously, iteration {iteration}...");

            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task<CacheResult<DataTable>>>();
            foreach (var table in tables)
            {
                var tmp = table; // Suggested by R#
                tasks.Add(Task.Factory.StartNew(() => PersistentCache.DefaultInstance.Get<DataTable>(tmp.TableName)));
            }
            foreach (var task in tasks)
            {
                var returnedTable = task.Result;
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine($@"Data tables retrieved in: {stopwatch.Elapsed}");
            Console.WriteLine($@"Current cache size: {PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreAndRetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"Storing and retrieving each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(Task.Run(() => PersistentCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            var readTasks = new List<Task<CacheResult<DataTable>>>();
            foreach (var table in tables)
            {
                var localTable = table;
                readTasks.Add(Task.Factory.StartNew(() => PersistentCache.DefaultInstance.Get<DataTable>(localTable.TableName)));
            }
            foreach (var task in writeTasks)
            {
                task.Wait();
            }
            foreach (var task in readTasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine($@"Data tables stored and retrieved in: {stopwatch.Elapsed}");
            Console.WriteLine($@"Current cache size: {PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"Approximate speed (MB/sec): {_tableListSize * 2 / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreAndRetrieveEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine($@"[Volatile] Storing and retrieving each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(Task.Run(() => VolatileCache.DefaultInstance.AddStatic(table.TableName, table)));
            }
            var readTasks = new List<Task<CacheResult<DataTable>>>();
            foreach (var table in tables)
            {
                var localTable = table;
                readTasks.Add(Task.Factory.StartNew(() => VolatileCache.DefaultInstance.Get<DataTable>(localTable.TableName)));
            }
            foreach (var task in writeTasks)
            {
                task.Wait();
            }
            foreach (var task in readTasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(VolatileCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(VolatileCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine($@"[Volatile] Data tables stored and retrieved in: {stopwatch.Elapsed}");
            Console.WriteLine($@"[Volatile] Current cache size: {VolatileCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L)} MB");
            Console.WriteLine($@"[Volatile] Approximate speed (MB/sec): {_tableListSize * 2 / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static double GetObjectSizeInMB(object obj)
        {
            using (var ms = new PooledMemoryStream())
            {
                new BinarySerializer().SerializeToStream(obj, ms);
                return ms.Length / (1024.0 * 1024.0);
            }
        }
    }
}
