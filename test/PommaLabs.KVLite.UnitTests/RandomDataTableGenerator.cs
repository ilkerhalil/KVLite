// File name: RandomDataTableGenerator.cs
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

using System;
using System.Data;
using System.Globalization;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests
{
    /// <summary>
    ///   Generates random data tables, starting from a given set of columns.
    /// </summary>
    public sealed class RandomDataTableGenerator
    {
        private readonly string[] _columnNames;
        private readonly Random _random = new Random();

        /// <summary>
        ///   Initializes the generator with a given set of columns.
        /// </summary>
        /// <param name="columnNames">The columns that the data tables will have.</param>
        public RandomDataTableGenerator(params string[] columnNames)
        {
            // Preconditions
            if (columnNames == null) throw new ArgumentNullException(nameof(columnNames));
            if (columnNames.Any(string.IsNullOrWhiteSpace)) throw new ArgumentException("Each column name cannot be null, empty or blank", nameof(columnNames));

            _columnNames = columnNames.Clone() as string[];
        }

        /// <summary>
        ///   Generates a new random data table, with given row count.
        /// </summary>
        /// <param name="rowCount">The number of rows the data table will have.</param>
        /// <returns>A new random data table, with given row count.</returns>
        public DataTable GenerateDataTable(int rowCount)
        {
            // Preconditions
            if (rowCount < 0) throw new ArgumentOutOfRangeException(nameof(rowCount));

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
