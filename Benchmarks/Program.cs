using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using KVLite;
using Thrower;

namespace Benchmarks
{
    public static class Program
    {
        private const int RandomDataTablesCount = 10;
        private const int RowCount = 10;

        private static readonly string[] ColumnNames = {"A", "B", "C", "D", "E"};

        public static void Main()
        {
            Console.WriteLine(@"Generating random data tables...");
            var tables = GenerateRandomDataTables();
            Console.WriteLine(@"Tables generated!");

            // Spacer
            Console.WriteLine();
            FullyCleanCache();
            Console.WriteLine();

            Console.WriteLine(@"Storing all data tables...");
            var elapsed = StoreAllDataTables(tables);
            Console.WriteLine(@"Data tables stored in: {0}", elapsed);

            // Spacer
            Console.WriteLine();
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
            Console.WriteLine(@"Fully cleaning cache...");
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
            Console.WriteLine(@"Cache cleaned!");
        }

        private static TimeSpan StoreAllDataTables(ICollection<DataTable> tables)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var table in tables) {
                PersistentCache.DefaultInstance.AddPersistent(table.TableName, table);
            }
            stopwatch.Stop();

            Debug.Assert(PersistentCache.DefaultInstance.Count() == tables.Count);
            Debug.Assert(PersistentCache.DefaultInstance.LongCount() == tables.LongCount());

            return stopwatch.Elapsed;
        }
    }

    public sealed class RandomDataTableGenerator
    {
        private readonly Random _random = new Random();
        private readonly string[] _columnNames;

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
            foreach (var columnName in _columnNames)
            {
                dt.Columns.Add(columnName);
            }
            for (var i = 0; i < rowCount; ++i)
            {
                var row = new object[_columnNames.Length];
                for (var j = 0; j < row.Length; ++j)
                {
                    row[j] = _random.Next(100, 999).ToString(CultureInfo.InvariantCulture);
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}
