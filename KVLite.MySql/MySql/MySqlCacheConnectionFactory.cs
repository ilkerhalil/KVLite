// File name: MySqlCacheConnectionFactory.cs
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

using LinqToDB.DataProvider;
using LinqToDB.DataProvider.MySql;
using LinqToDB.Mapping;
using MySql.Data.MySqlClient;
using PommaLabs.KVLite.Core;
using System.Data;

namespace PommaLabs.KVLite.MySql
{
    internal sealed class MySqlCacheConnectionFactory : DbCacheConnectionFactory<MySqlConnection>
    {
        public MySqlCacheConnectionFactory()
            : base(MySqlClientFactory.Instance)
        {
            RetrieveOneItemByHash = MinifyQuery($@"
                select x.kvli_partition
                  from {CacheSchemaName}.{CacheItemsTableName} x
                 where x.kvli_hash = @hash
            ");
        }

        public string CacheSchemaName { get; set; } = "kvlite";

        public string CacheItemsTableName { get; set; } = "kvl_cache_items";

        /// <summary>
        ///   The data provider for which connections are opened.
        /// </summary>
        public IDataProvider DataProvider { get; } = new MySqlDataProvider();

        public MappingSchema MappingSchema { get; }

        public long GetCacheSizeInKB()
        {
            using (var connection = Create())
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = $@"
                    select round(sum(length(kvli_value)) / 1024) as result
                    from {CacheSchemaName}.{CacheItemsTableName};
                ";

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    reader.Read();
                    return reader.GetInt64(0);
                }
            }
        }

        #region Queries

        public string RetrieveOneItemByHash { get; }

        #endregion
    }
}