using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using PommaLabs.GRAMPA.Testing;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Properties;

namespace Benchmarks
{
    public static class Program
    {
        private const int RowCount = 1000;
        private const int IterationCount = 5;
        
        private static readonly int RandomDataTablesCount = Settings.Default.PCache_DefaultInsertionCountBeforeCleanup * 10;

        private static readonly string[] ColumnNames = {"A", "B", "C", "D", "E"};

        private static double _tableListSize;

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

            for (var i = 0; i < IterationCount; ++i) {
                FullyCleanCache();
                StoreEachDataTable(tables, i);

                FullyCleanCache();
                RetrieveEachDataTable(tables, i);

                FullyCleanCache();
                StoreEachDataTableAsync(tables, i);

                FullyCleanCache();
                RetrieveEachDataTableAsync(tables, i);

                FullyCleanCache();
                StoreEachDataTableTwoTimesAsync(tables, i);

                FullyCleanCache();
                StoreAndRetrieveEachDataTableAsync(tables, i);

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

        private static IList<DataTable> GenerateRandomDataTables()
        {
            var gen = new RandomDataTableGenerator(ColumnNames);
            var list = new List<DataTable>();
            for (var i = 0; i < RandomDataTablesCount; ++i) {
                list.Add(gen.GenerateDataTable(RowCount));
            }
            return list;
        }

        private static void FullyCleanCache()
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Fully cleaning cache...");
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.IgnoreExpiryDate);
            Console.WriteLine(@"Cache cleaned!");
        }

        private static void StoreDataTableList(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing data table list, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            PersistentCache.DefaultInstance.AddStatic("TABLE_LIST", tables);
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 1);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 1);

            Console.WriteLine(@"Data table list stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddStatic(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables) {
                tasks.Add(PersistentCache.DefaultInstance.AddStaticAsync(table.TableName, table));
            }
            foreach (var task in tasks) {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreEachDataTableTwoTimesAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing each data table (two times, asynchronously), iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables) {
                tasks.Add(PersistentCache.DefaultInstance.AddStaticAsync(table.TableName, table));
            }
            foreach (var table in tables) {
                tasks.Add(PersistentCache.DefaultInstance.AddStaticAsync(table.TableName, table));
            }
            foreach (var task in tasks) {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize * 2/stopwatch.Elapsed.TotalSeconds);
        }

        private static void RetrieveEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Retrieving each data table, iteration {0}...", iteration);

            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables) {
                var returnedTable = PersistentCache.DefaultInstance.Get<DataTable>(table.TableName);
                if (returnedTable == null || returnedTable.TableName != table.TableName) {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }            
            stopwatch.Stop();

            Console.WriteLine(@"Data tables retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void RetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Retrieving each data table asynchronously, iteration {0}...", iteration);

            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddStatic(table.TableName, table);
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task<object>>();
            foreach (var table in tables) {
                tasks.Add(Task.Run(() => PersistentCache.DefaultInstance.Get(table.TableName)));
            }
            foreach (var task in tasks) {
                var returnedTable = task.Result as DataTable;
                if (returnedTable == null) {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            stopwatch.Stop();

            Console.WriteLine(@"Data tables retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void StoreAndRetrieveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Storing and retrieving each data table asynchronously, iteration {0}...", iteration);

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var writeTasks = new List<Task>();
            foreach (var table in tables) {
                writeTasks.Add(PersistentCache.DefaultInstance.AddStaticAsync(table.TableName, table));
            }
            var readTasks = new List<Task<object>>();
            foreach (var table in tables) {
                readTasks.Add(Task.Run(() => PersistentCache.DefaultInstance.Get(table.TableName)));
            }
            foreach (var task in writeTasks) {
                task.Wait();
            }
            foreach (var task in readTasks) {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables stored and retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize*2/stopwatch.Elapsed.TotalSeconds);
        }

        private static void RemoveEachDataTable(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Removing each data table, iteration {0}...", iteration);

            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddTimed(table.TableName, table, DateTime.UtcNow.AddHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables) {
                PersistentCache.DefaultInstance.Remove(table.TableName);
            }            
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 0);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 0);

            Console.WriteLine(@"Data tables removed in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static void RemoveEachDataTableAsync(ICollection<DataTable> tables, int iteration)
        {
            Console.WriteLine(); // Spacer
            Console.WriteLine(@"Removing each data table asynchronously, iteration {0}...", iteration);

            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddTimed(table.TableName, table, DateTime.UtcNow.AddHours(1));
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var tasks = new List<Task>();
            foreach (var table in tables) {
                tasks.Add(PersistentCache.DefaultInstance.RemoveAsync(table.TableName));
            }
            foreach (var task in tasks) {
                task.Wait();
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == 0);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == 0);

            Console.WriteLine(@"Data tables removed in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.TotalSeconds);
        }

        private static double GetObjectSizeInMB(object obj)
        {
            double size;
            using (var s = new MemoryStream()) {
                var formatter = new BinaryFormatter {TypeFormat = FormatterTypeStyle.TypesWhenNeeded};
                formatter.Serialize(s, obj);
                size = s.Length;
            }
            return size/(1024.0*1024.0);
        }
    }
}