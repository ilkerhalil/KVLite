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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using BenchmarkDotNet.Running;
using PommaLabs.CodeServices.Caching;
using PommaLabs.CodeServices.Common;
using PommaLabs.CodeServices.Common.Threading.Tasks;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.Oracle;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.SqlServer;
using PommaLabs.KVLite.UnitTests;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.Benchmarks
{
    public static class Program
    {
        private const int RowCount = 100;
        private const int IterationCount = 5;
        private const int RandomDataTablesCount = 1000;

        private static readonly string[] ColumnNames = { "A", "B", "C", "D", "E" };

        private static double _tableListSize;

        public static void Main(string[] args)
        {
            if (args.Length == 1 && args[0].Equals(nameof(Comparison)))
            {
                BenchmarkRunner.Run<Comparison>();
                return;
            }

            MySqlCache.DefaultInstance.ConnectionFactory.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(MySql)].ConnectionString;
            SqlServerCache.DefaultInstance.ConnectionFactory.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(SqlServer)].ConnectionString;

            OracleCache.DefaultInstance.ConnectionFactory.CacheSchemaName = "CARAVAN";
            OracleCache.DefaultInstance.ConnectionFactory.CacheEntriesTableName = "CRVN_KVL_ENTRIES";
            OracleCache.DefaultInstance.ConnectionFactory.ConnectionString = ConfigurationManager.ConnectionStrings[nameof(Oracle)].ConnectionString;

            Console.WriteLine(@"Running vacuum on DB...");
            PersistentCache.DefaultInstance.Vacuum();
            Console.WriteLine(@"Vacuum completed.");

            Console.WriteLine();
            Console.WriteLine(@"Generating random data tables...");
            var tables = GenerateRandomDataTables();
            _tableListSize = GetObjectSizeInMB(tables);
            GC.Collect();
            Console.WriteLine(@"Tables generated!");
            Console.WriteLine(@"Table Count: {0}", RandomDataTablesCount);
            Console.WriteLine(@"Row Count: {0}", RowCount);
            Console.WriteLine(@"Total Size: {0:0.0} MB", _tableListSize);

            for (var i = 0; i < IterationCount; ++i)
            {
                /*** STORE EACH DATA TABLE ASYNC ***/

                FullyCleanCache();
                //StoreEachDataTableAsync(OracleCache.DefaultInstance, tables, i);
                StoreEachDataTableAsync(MySqlCache.DefaultInstance, tables, i);
                StoreEachDataTableAsync(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync(MemoryCache.DefaultInstance, tables, i);

                /*** STORE EACH DATA TABLE ***/

                FullyCleanCache();
                //StoreEachDataTable(OracleCache.DefaultInstance, tables, i);
                StoreEachDataTable(MySqlCache.DefaultInstance, tables, i);
                StoreEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** STORE EACH DATA TABLE TWO TIMES ***/

                FullyCleanCache();
                //StoreEachDataTableTwoTimes(OracleCache.DefaultInstance, tables, i);
                StoreEachDataTableTwoTimes(MySqlCache.DefaultInstance, tables, i);
                StoreEachDataTableTwoTimes(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableTwoTimes(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableTwoTimes(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                StoreEachDataTableTwoTimes(MemoryCache.DefaultInstance, tables, i);

                /*** REMOVE EACH DATA TABLE ***/

                FullyCleanCache();
                //RemoveEachDataTable(OracleCache.DefaultInstance, tables, i);
                RemoveEachDataTable(MySqlCache.DefaultInstance, tables, i);
                RemoveEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** REMOVE EACH DATA TABLE ASYNC ***/

                FullyCleanCache();
                RemoveEachDataTableAsync(MySqlCache.DefaultInstance, tables, i);
                RemoveEachDataTableAsync(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTableAsync(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTableAsync(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RemoveEachDataTableAsync(MemoryCache.DefaultInstance, tables, i);

                /*** PEEK EACH DATA TABLE ***/

                FullyCleanCache();
                PeekEachDataTable(MySqlCache.DefaultInstance, tables, i);
                PeekEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                PeekEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                PeekEachDataTable(VolatileCache.DefaultInstance, tables, i);

                //FullyCleanCache(); < --Memory cache does not allow peeking!
                //PeekEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** RETRIEVE EACH DATA TABLE ***/

                FullyCleanCache();
                //RetrieveEachDataTable(OracleCache.DefaultInstance, tables, i);
                RetrieveEachDataTable(MySqlCache.DefaultInstance, tables, i);
                RetrieveEachDataTable(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTable(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTable(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTable(MemoryCache.DefaultInstance, tables, i);

                /*** RETRIEVE EACH DATA TABLE ITEM ***/

                FullyCleanCache();
                RetrieveEachDataTableItem(MySqlCache.DefaultInstance, tables, i);
                RetrieveEachDataTableItem(SqlServerCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTableItem(PersistentCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTableItem(VolatileCache.DefaultInstance, tables, i);

                FullyCleanCache();
                RetrieveEachDataTableItem(MemoryCache.DefaultInstance, tables, i);

                /*** TODO ***/

                FullyCleanCache();
                StoreEachDataTableAsync_Volatile(tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync_Memory(tables, i);

                FullyCleanCache();
                RetrieveEachDataTableAsync(tables, i);

                FullyCleanCache();
                StoreEachDataTableTwoTimesAsync(tables, i);

                FullyCleanCache();
                StoreAndRetrieveEachDataTableAsync(tables, i);

                FullyCleanCache();
                StoreAndRetrieveEachDataTableAsync_Volatile(tables, i);

                FullyCleanCache();
                StoreDataTableList(tables, i);
            }

            FullyCleanCache();

            Console.WriteLine();
            Console.Write(@"Press any key to exit...");
            Console.Read();
        }

        private static IList<DataTable> GenerateRandomDataTables()
        {
            var gen = new RandomDataTableGenerator(ColumnNames);
            var list = new List<DataTable>();
            for (var i = 0; i < RandomDataTablesCount; ++i)
            {
                list.Add(gen.GenerateDataTable(RowCount));
            }
            return list;
        }

        private static void FullyCleanCache()
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Fully cleaning cache...");
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            VolatileCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            MySqlCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            OracleCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            SqlServerCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            MemoryCache.DefaultInstance.Clear();
            Console.WriteLine(@"Cache cleaned!");
        }

        private static void StoreDataTableList(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing data table list, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            PersistentCache.DefaultInstance.AddStaticToDefaultPartition("TABLE_LIST", tables);
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 1);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 1);

            Console.WriteLine(@"Data table list stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"Approximate speed (MB/sec): {0:0.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
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
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table two times, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
            }
            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(cache.Count() == tables.Count);
            Debug.Assert(cache.LongCount() == tables.LongCount());

            Console.WriteLine($"[{cacheName}] Data tables stored two times in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytes() / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableAsync<TCache>(TCache cache, IList<DataTable> tables, int iteration)
            where TCache : IAsyncCache
        {
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Storing each data table asynchronously, iteration {iteration}...");

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new Task[tables.Count];
            for (var i = 0; i < tables.Count; ++i)
            {
                var table = tables[i];
                tasks[i] = Task.Run(async () => await cache.AddStaticToDefaultPartitionAsync(table.TableName, table));
            }
            Task.WaitAll(tasks);
            stopwatch.Stop();

            Debug.Assert(cache.CountAsync().Result == tables.Count);
            Debug.Assert(cache.LongCountAsync().Result == tables.LongCount());

            Console.WriteLine($"[{cacheName}] Data tables stored in: {stopwatch.Elapsed}");
            Console.WriteLine($"[{cacheName}] Current cache size: {cache.GetCacheSizeInBytesAsync().Result / (1024.0 * 1024.0):0.0} MB");
            Console.WriteLine($"[{cacheName}] Approximate speed (MB/sec): {_tableListSize / stopwatch.Elapsed.TotalSeconds:0.0}");
        }

        private static void StoreEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Volatile] Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.RunAsync(() => VolatileCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(VolatileCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(VolatileCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Volatile] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Volatile] Current cache size: {0} MB", VolatileCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"[Volatile] Approximate speed (MB/sec): {0:0.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTableAsync_Memory(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Memory] Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.RunAsync(() => MemoryCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(MemoryCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(MemoryCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Memory] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Memory] Current cache size: {0} MB", MemoryCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"[Memory] Approximate speed (MB/sec): {0:0.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTableTwoTimesAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table (two times, asynchronously), iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.RunAsync(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.RunAsync(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"Approximate speed (MB/sec): {0:0.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        private static void RetrieveEachDataTable<TCache>(TCache cache, ICollection<DataTable> tables, int iteration)
            where TCache : ICache
        {
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Retrieving each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.GetFromDefaultPartition<DataTable>(table.TableName);
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
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Retrieving each data table item, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.GetItemFromDefaultPartition<DataTable>(table.TableName);
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
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Peeking each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddStaticToDefaultPartition(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = cache.PeekIntoDefaultPartition<DataTable>(table.TableName);
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
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Removing each data table, iteration {iteration}...");

            foreach (var table in tables)
            {
                cache.AddTimedToDefaultPartition(table.TableName, table, DateTime.UtcNow.AddHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                cache.RemoveFromDefaultPartition(table.TableName);
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
            var cacheName = typeof(TCache).Name;

            Console.WriteLine(); // Spacer
            Console.WriteLine($"[{cacheName}] Removing each data table asynchronously, iteration {iteration}...");

            var tasks = new Task[tables.Count];
            for (var i = 0; i < tables.Count; ++i)
            {
                var table = tables[i];
                tasks[i] = Task.Run(async () => await cache.AddTimedToDefaultPartitionAsync(table.TableName, table, DateTime.UtcNow.AddHours(1)));
            }
            Task.WaitAll(tasks);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (var i = 0; i < tables.Count; ++i)
            {
                var table = tables[i];
                tasks[i] = Task.Run(async () => await cache.RemoveFromDefaultPartitionAsync(table.TableName));
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
            Console.WriteLine(@"Retrieving each data table asynchronously, iteration {0}...", iteration);

            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task<Option<DataTable>>>();
            foreach (var table in tables)
            {
                var tmp = table; // Suggested by R#
                tasks.Add(Task.Factory.StartNew(() => PersistentCache.DefaultInstance.GetFromDefaultPartition<DataTable>(tmp.TableName)));
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

            Console.WriteLine(@"Data tables retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"Approximate speed (MB/sec): {0:0.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreAndRetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing and retrieving each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(TaskHelper.RunAsync(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            var readTasks = new List<Task<Option<DataTable>>>();
            foreach (var table in tables)
            {
                var localTable = table;
                readTasks.Add(Task.Factory.StartNew(() => PersistentCache.DefaultInstance.GetFromDefaultPartition<DataTable>(localTable.TableName)));
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

            Console.WriteLine(@"Data tables stored and retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"Approximate speed (MB/sec): {0:0.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreAndRetrieveEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Volatile] Storing and retrieving each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(TaskHelper.RunAsync(() => VolatileCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            var readTasks = new List<Task<Option<DataTable>>>();
            foreach (var table in tables)
            {
                var localTable = table;
                readTasks.Add(Task.Factory.StartNew(() => VolatileCache.DefaultInstance.GetFromDefaultPartition<DataTable>(localTable.TableName)));
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

            Console.WriteLine(@"[Volatile] Data tables stored and retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Volatile] Current cache size: {0} MB", VolatileCache.DefaultInstance.GetCacheSizeInBytes() / (1024L * 1024L));
            Console.WriteLine(@"[Volatile] Approximate speed (MB/sec): {0:0.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        private static double GetObjectSizeInMB(object obj)
        {
            using (var s = new BinarySerializer().SerializeToStream(obj))
            {
                return s.Length / (1024.0 * 1024.0);
            }
        }
    }
}