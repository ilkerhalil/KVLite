using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using KVLite;
using Thrower;

namespace Benchmarks
{
    public static class Program
    {
        private const int RandomDataTablesCount = 10;
        private const int RowCount = 1000;
        private const int IterationCount = 5;

        private static readonly string[] ColumnNames = {"A", "B", "C", "D", "E"};

        private static double _tableListSize;

        public static void Main()
        {
            Console.WriteLine(@"Generating random data tables...");
            var tables = GenerateRandomDataTables();
            _tableListSize = GetObjectSizeInMB(tables);
            Console.WriteLine(@"Tables generated, size in MB: {0}", _tableListSize);

            for (var i = 0; i < IterationCount; ++i) {
                FullyCleanCache();
                RetrieveEachDataTable(tables, i);
            }

            for (var i = 0; i < IterationCount; ++i) {
                FullyCleanCache();
                StoreDataTableList(tables, i);
            }

            for (var i = 0; i < IterationCount; ++i) {
                FullyCleanCache();
                StoreEachDataTable(tables, i);
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
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
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
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.Seconds);
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
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.Seconds);
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
                var returnedTable = PersistentCache.DefaultInstance.Get(table.TableName) as DataTable;
                if (returnedTable == null || returnedTable.TableName != table.TableName) {
                    throw new Exception("Wrong data table read from cache! :(");
                }
            }
            
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            Console.WriteLine(@"Data tables retrieved in: {0}", stopwatch.Elapsed);
            Console.WriteLine(@"Approximate speed (MB/sec): {0}", _tableListSize/stopwatch.Elapsed.Seconds);
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

    public sealed class RandomDataTableGenerator
    {
        private readonly string[] _columnNames;
        private readonly Random _random = new Random();

        public RandomDataTableGenerator(params string[] columnNames)
        {
            Raise<ArgumentNullException>.IfIsNull(columnNames);
            Raise<ArgumentException>.If(columnNames.Any(String.IsNullOrEmpty));

            _columnNames = columnNames.Clone() as string[];
        }

        public DataTable GenerateDataTable(int rowCount)
        {
            Raise<ArgumentOutOfRangeException>.If(rowCount < 0);

            var dt = new DataTable("RANDOMLY_GENERATED_DATA_TABLE_" + _random.Next());
            foreach (var columnName in _columnNames) {
                dt.Columns.Add(columnName);
            }
            for (var i = 0; i < rowCount; ++i) {
                var row = new object[_columnNames.Length];
                for (var j = 0; j < row.Length; ++j) {
                    row[j] = _random.Next(100, 999).ToString(CultureInfo.InvariantCulture);
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}