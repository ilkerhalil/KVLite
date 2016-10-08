// File name: MySqlDbCacheConnectionFactory.cs
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

using System;
using System.Configuration;
using System.Data;

namespace PommaLabs.KVLite.Benchmarks.ConnectionFactories
{
    internal sealed class MySqlDbCacheConnectionFactory : IDbCacheConnectionFactory
    {
        private static readonly string ConnectionString = ConfigurationManager.ConnectionStrings["MySQL"].ConnectionString;

        public static MySqlDbCacheConnectionFactory Instance { get; } = new MySqlDbCacheConnectionFactory();

        public string CacheSchemaName { get; } = "kvlite";

        public string CacheItemsTableName { get; } = "kvl_cache_items";

        public string CacheSettingsTableName { get; } = "kvl_cache_settings";

        public DbCacheConnectionProvider Provider { get; } = DbCacheConnectionProvider.MySQL;

        public IDbConnection Create()
        {
            var connection = MySql.Data.MySqlClient.MySqlClientFactory.Instance.CreateConnection();
            connection.ConnectionString = ConnectionString;
            return connection;
        }

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
    }
}