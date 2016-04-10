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
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Finsa.CodeServices.Common;
using PommaLabs.KVLite.UnitTests;
using Finsa.CodeServices.Serialization;
using Finsa.CodeServices.Common.Threading.Tasks;

namespace PommaLabs.KVLite.Benchmarks
{
    public static class Program
    {
        const int RowCount = 1000;
        const int IterationCount = 5;
        const int RandomDataTablesCount = 1000;

        static readonly string[] ColumnNames = { "A", "B", "C", "D", "E" };

        static double _tableListSize;

        public static void Main()
        {
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
            Console.WriteLine(@"Total Size: {0:.0} MB", _tableListSize);

            for (var i = 0; i < IterationCount; ++i)
            {
                FullyCleanCache();
                StoreEachDataTable(tables, i);

                FullyCleanCache();
                StoreEachDataTable_Volatile(tables, i);

                FullyCleanCache();
                StoreEachDataTable_Memory(tables, i);

                FullyCleanCache();
                RetrieveEachDataTable(tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync(tables, i);

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
                RemoveEachDataTable(tables, i);

                FullyCleanCache();
                RemoveEachDataTableAsync(tables, i);

                FullyCleanCache();
                StoreDataTableList(tables, i);
            }

            FullyCleanCache();

            Console.WriteLine();
            Console.Write(@"Press any key to exit...");
            Console.Read();
        }

        static IList<DataTable> GenerateRandomDataTables()
        {
            var gen = new RandomDataTableGenerator(ColumnNames);
            var list = new List<DataTable>();
            for (var i = 0; i < RandomDataTablesCount; ++i)
            {
                list.Add(gen.GenerateDataTable(RowCount));
            }
            return list;
        }

        static void FullyCleanCache()
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Fully cleaning cache...");
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            VolatileCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            MemoryCache.DefaultInstance.Clear();
            Console.WriteLine(@"Cache cleaned!");
        }

        static void StoreDataTableList(ICollection<DataTable> tables, int iteration)
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
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTable_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Volatile] Storing each data table, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                VolatileCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(VolatileCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(VolatileCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Volatile] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Volatile] Current cache size: {0} MB", VolatileCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"[Volatile] Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTable_Memory(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Memory] Storing each data table, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                MemoryCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(MemoryCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(MemoryCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Memory] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Memory] Current cache size: {0} MB", MemoryCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"[Memory] Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Volatile] Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => VolatileCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(VolatileCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(VolatileCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Volatile] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Volatile] Current cache size: {0} MB", VolatileCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"[Volatile] Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTableAsync_Memory(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Memory] Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => MemoryCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(MemoryCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(MemoryCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"[Memory] Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"[Memory] Current cache size: {0} MB", MemoryCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"[Memory] Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreEachDataTableTwoTimesAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table (two times, asynchronously), iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        static void RetrieveEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Retrieving each data table, iteration {0}...", iteration);

            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                var returnedTable = PersistentCache.DefaultInstance.GetFromDefaultPartition<DataTable>(table.TableName);
                if (!returnedTable.HasValue)
                {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine(@"Data tables retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void RetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
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
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreAndRetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing and retrieving each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(TaskHelper.Run(() => PersistentCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
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
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        static void StoreAndRetrieveEachDataTableAsync_Volatile(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"[Volatile] Storing and retrieving each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables)
            {
                writeTasks.Add(TaskHelper.Run(() => VolatileCache.DefaultInstance.AddStaticToDefaultPartition(table.TableName, table)));
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
            Console.WriteLine(@"[Volatile] Current cache size: {0} MB", VolatileCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"[Volatile] Approximate speed (MB/sec): {0:.0}", _tableListSize * 2 / stopwatch.Elapsed.TotalSeconds);
        }

        static void RemoveEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Removing each data table, iteration {0}...", iteration);

            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddTimedToDefaultPartition(table.TableName, table, DateTime.UtcNow.AddHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.RemoveFromDefaultPartition(table.TableName);
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 0);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 0);

            Console.WriteLine(@"Data tables removed in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static void RemoveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Removing each data table asynchronously, iteration {0}...", iteration);

            foreach (var table in tables)
            {
                PersistentCache.DefaultInstance.AddTimedToDefaultPartition(table.TableName, table, DateTime.UtcNow.AddHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables)
            {
                tasks.Add(TaskHelper.Run(() => PersistentCache.DefaultInstance.RemoveFromDefaultPartition(table.TableName)));
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 0);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 0);

            Console.WriteLine(@"Data tables removed in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Current cache size: {0} MB", PersistentCache.DefaultInstance.CacheSizeInKB() / 1024L);
            Console.WriteLine(@"Approximate speed (MB/sec): {0:.0}", _tableListSize / stopwatch.Elapsed.TotalSeconds);
        }

        static double GetObjectSizeInMB(object obj)
        {
            using (var s = new BinarySerializer().SerializeToStream(obj))
            {
                return s.Length / (1024.0 * 1024.0);
            }
        }
    }
}