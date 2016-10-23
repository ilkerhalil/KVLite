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

using PommaLabs.KVLite.Core;
using System;
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace PommaLabs.KVLite.MySql
{
    public class MySqlCacheConnectionFactory : DbCacheConnectionFactory
    {
        private static readonly DbProviderFactory DbProviderFactory;

        static MySqlCacheConnectionFactory()
        {
            try
            {
                var factoryType = Type.GetType("MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data");
                DbProviderFactory = factoryType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null) as DbProviderFactory;
            }
            catch (Exception ex)
            {
                throw new Exception(ErrorMessages.MissingMySqlDriver, ex);
            }
        }


        public MySqlCacheConnectionFactory()
            : base(DbProviderFactory, null, null, null)
        {
            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert into {CacheSchemaName}.{CacheItemsTableName} (
                    {DbCacheEntry.HashColumn},
                    {DbCacheEntry.PartitionColumn}, {DbCacheEntry.KeyColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.UtcExpiryColumn}, {DbCacheEntry.IntervalColumn},
                    {DbCacheEntry.ParentHash0Column}, {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column}, {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column}, {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentHash3Column}, {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentHash4Column}, {DbCacheEntry.ParentKey4Column}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)},
                    @{nameof(DbCacheEntry.Partition)}, @{nameof(DbCacheEntry.Key)},
                    @{nameof(DbCacheEntry.UtcCreation)},
                    @{nameof(DbCacheEntry.UtcExpiry)}, @{nameof(DbCacheEntry.Interval)},
                    @{nameof(DbCacheEntry.ParentHash0)}, @{nameof(DbCacheEntry.ParentKey0)},
                    @{nameof(DbCacheEntry.ParentHash1)}, @{nameof(DbCacheEntry.ParentKey1)},
                    @{nameof(DbCacheEntry.ParentHash2)}, @{nameof(DbCacheEntry.ParentKey2)},
                    @{nameof(DbCacheEntry.ParentHash3)}, @{nameof(DbCacheEntry.ParentKey3)},
                    @{nameof(DbCacheEntry.ParentHash4)}, @{nameof(DbCacheEntry.ParentKey4)}
                )
                on duplicate key update
                    {DbCacheEntry.UtcCreationColumn} = @{nameof(DbCacheEntry.UtcCreation)},
                    {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.UtcExpiry)},
                    {DbCacheEntry.IntervalColumn} = @{nameof(DbCacheEntry.Interval)},
                    {DbCacheEntry.ParentHash0Column} = @{nameof(DbCacheEntry.ParentHash0)},
                    {DbCacheEntry.ParentKey0Column} = @{nameof(DbCacheEntry.ParentKey0)},
                    {DbCacheEntry.ParentHash1Column} = @{nameof(DbCacheEntry.ParentHash1)},
                    {DbCacheEntry.ParentKey1Column} = @{nameof(DbCacheEntry.ParentKey1)},
                    {DbCacheEntry.ParentHash2Column} = @{nameof(DbCacheEntry.ParentHash2)},
                    {DbCacheEntry.ParentKey2Column} = @{nameof(DbCacheEntry.ParentKey2)},
                    {DbCacheEntry.ParentHash3Column} = @{nameof(DbCacheEntry.ParentHash3)},
                    {DbCacheEntry.ParentKey3Column} = @{nameof(DbCacheEntry.ParentKey3)},
                    {DbCacheEntry.ParentHash4Column} = @{nameof(DbCacheEntry.ParentHash4)},
                    {DbCacheEntry.ParentKey4Column} = @{nameof(DbCacheEntry.ParentKey4)};

                insert into {CacheSchemaName}.{CacheValuesTableName} (
                    {DbCacheEntry.HashColumn},
                    {DbCacheEntry.ValueColumn},
                    {DbCacheEntry.CompressedColumn}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)},
                    @{nameof(DbCacheEntry.Value)},
                    @{nameof(DbCacheEntry.Compressed)}
                )
                on duplicate key update
                    {DbCacheEntry.ValueColumn} = @{nameof(DbCacheEntry.Value)},
                    {DbCacheEntry.CompressedColumn} = @{nameof(DbCacheEntry.Compressed)};
            ");

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheItemsTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntryQuery = MinifyQuery($@"
                select item.{DbCacheEntry.PartitionColumn} `{nameof(DbCacheEntry.Partition)}`,
                       item.{DbCacheEntry.KeyColumn} `{nameof(DbCacheEntry.Key)}`,
                       item.{DbCacheEntry.UtcCreationColumn} `{nameof(DbCacheEntry.UtcCreation)}`,
                       item.{DbCacheEntry.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       item.{DbCacheEntry.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       item.{DbCacheEntry.ParentHash0Column} `{nameof(DbCacheEntry.ParentHash0)}`,
                       item.{DbCacheEntry.ParentKey0Column} `{nameof(DbCacheEntry.ParentKey0)}`,
                       item.{DbCacheEntry.ParentHash1Column} `{nameof(DbCacheEntry.ParentHash1)}`,
                       item.{DbCacheEntry.ParentKey1Column} `{nameof(DbCacheEntry.ParentKey1)}`,
                       item.{DbCacheEntry.ParentHash2Column} `{nameof(DbCacheEntry.ParentHash2)}`,
                       item.{DbCacheEntry.ParentKey2Column} `{nameof(DbCacheEntry.ParentKey2)}`,
                       item.{DbCacheEntry.ParentHash3Column} `{nameof(DbCacheEntry.ParentHash3)}`,
                       item.{DbCacheEntry.ParentKey3Column} `{nameof(DbCacheEntry.ParentKey3)}`,
                       item.{DbCacheEntry.ParentHash4Column} `{nameof(DbCacheEntry.ParentHash4)}`,
                       item.{DbCacheEntry.ParentKey4Column} `{nameof(DbCacheEntry.ParentKey4)}`,
                       value.{DbCacheEntry.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       value.{DbCacheEntry.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`
                  from {CacheSchemaName}.{CacheItemsTableName} item natural join {CacheSchemaName}.{CacheValuesTableName} value
                 where item.{DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or item.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select entry.`{nameof(DbCacheEntry.UtcExpiry)}` `{nameof(Core.DbCacheValue.UtcExpiry)}`,
                       entry.`{nameof(DbCacheEntry.Interval)}` `{nameof(Core.DbCacheValue.Interval)}`,
                       entry.`{nameof(DbCacheEntry.Value)}` `{nameof(Core.DbCacheValue.Value)}`,
                       entry.`{nameof(DbCacheEntry.Compressed)}` `{nameof(Core.DbCacheValue.Compressed)}`
                  from ({PeekCacheEntryQuery}) entry
            ");

            #endregion Queries
        }

        public override long GetCacheSizeInBytes()
        {
            using (var connection = Create())
            using (var command = connection.CreateCommand())
            {
                command.CommandType = CommandType.Text;
                command.CommandText = $@"
                    select round(sum(length(kvlv_value))) as result
                    from {CacheSchemaName}.{CacheValuesTableName};
                ";

                connection.Open();
                using (var reader = command.ExecuteReader())
                {
                    return (reader.Read() && !reader.IsDBNull(0)) ? reader.GetInt64(0) : 0L;
                }
            }
        }
    }
}