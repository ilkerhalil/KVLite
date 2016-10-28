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
                insert into {CacheSchemaName}.{CacheEntriesTableName} (
                    {DbCacheEntry.HashColumn}, {DbCacheEntry.UtcExpiryColumn}, {DbCacheEntry.IntervalColumn},
                    {DbCacheEntry.ValueColumn}, {DbCacheEntry.CompressedColumn},
                    {DbCacheEntry.PartitionColumn}, {DbCacheEntry.KeyColumn}, {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentHash0Column}, {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column}, {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column}, {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentHash3Column}, {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentHash4Column}, {DbCacheEntry.ParentKey4Column}
                )
                values (
                    @{nameof(DbCacheEntry.Hash)}, @{nameof(DbCacheEntry.UtcExpiry)}, @{nameof(DbCacheEntry.Interval)},
                    @{nameof(DbCacheEntry.Value)}, @{nameof(DbCacheEntry.Compressed)},
                    @{nameof(DbCacheEntry.Partition)}, @{nameof(DbCacheEntry.Key)}, @{nameof(DbCacheEntry.UtcCreation)},
                    @{nameof(DbCacheEntry.ParentHash0)}, @{nameof(DbCacheEntry.ParentKey0)},
                    @{nameof(DbCacheEntry.ParentHash1)}, @{nameof(DbCacheEntry.ParentKey1)},
                    @{nameof(DbCacheEntry.ParentHash2)}, @{nameof(DbCacheEntry.ParentKey2)},
                    @{nameof(DbCacheEntry.ParentHash3)}, @{nameof(DbCacheEntry.ParentKey3)},
                    @{nameof(DbCacheEntry.ParentHash4)}, @{nameof(DbCacheEntry.ParentKey4)}
                )
                on duplicate key update
                    {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.UtcExpiry)},
                    {DbCacheEntry.IntervalColumn} = @{nameof(DbCacheEntry.Interval)},
                    {DbCacheEntry.ValueColumn} = @{nameof(DbCacheEntry.Value)},
                    {DbCacheEntry.CompressedColumn} = @{nameof(DbCacheEntry.Compressed)},
                    {DbCacheEntry.UtcCreationColumn} = @{nameof(DbCacheEntry.UtcCreation)},
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
            ");

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheEntriesTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheEntriesTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            UpdateCacheEntryExpiryCommand = MinifyQuery($@"
                update {CacheSchemaName}.{CacheEntriesTableName}
                   set {DbCacheEntry.UtcExpiryColumn} = @{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheEntriesTableName}
                 where {DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheEntriesTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntriesQuery = MinifyQuery($@"
                select x.{DbCacheEntry.PartitionColumn} `{nameof(DbCacheEntry.Partition)}`,
                       x.{DbCacheEntry.KeyColumn} `{nameof(DbCacheEntry.Key)}`,
                       x.{DbCacheEntry.UtcCreationColumn} `{nameof(DbCacheEntry.UtcCreation)}`,
                       x.{DbCacheEntry.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       x.{DbCacheEntry.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       x.{DbCacheEntry.ParentHash0Column} `{nameof(DbCacheEntry.ParentHash0)}`,
                       x.{DbCacheEntry.ParentKey0Column} `{nameof(DbCacheEntry.ParentKey0)}`,
                       x.{DbCacheEntry.ParentHash1Column} `{nameof(DbCacheEntry.ParentHash1)}`,
                       x.{DbCacheEntry.ParentKey1Column} `{nameof(DbCacheEntry.ParentKey1)}`,
                       x.{DbCacheEntry.ParentHash2Column} `{nameof(DbCacheEntry.ParentHash2)}`,
                       x.{DbCacheEntry.ParentKey2Column} `{nameof(DbCacheEntry.ParentKey2)}`,
                       x.{DbCacheEntry.ParentHash3Column} `{nameof(DbCacheEntry.ParentHash3)}`,
                       x.{DbCacheEntry.ParentKey3Column} `{nameof(DbCacheEntry.ParentKey3)}`,
                       x.{DbCacheEntry.ParentHash4Column} `{nameof(DbCacheEntry.ParentHash4)}`,
                       x.{DbCacheEntry.ParentKey4Column} `{nameof(DbCacheEntry.ParentKey4)}`,
                       x.{DbCacheEntry.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       x.{DbCacheEntry.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`
                  from {CacheSchemaName}.{CacheEntriesTableName} x
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or x.{DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or x.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntryQuery = MinifyQuery($@"
                select x.{DbCacheEntry.PartitionColumn} `{nameof(DbCacheEntry.Partition)}`,
                       x.{DbCacheEntry.KeyColumn} `{nameof(DbCacheEntry.Key)}`,
                       x.{DbCacheEntry.UtcCreationColumn} `{nameof(DbCacheEntry.UtcCreation)}`,
                       x.{DbCacheEntry.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       x.{DbCacheEntry.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       x.{DbCacheEntry.ParentHash0Column} `{nameof(DbCacheEntry.ParentHash0)}`,
                       x.{DbCacheEntry.ParentKey0Column} `{nameof(DbCacheEntry.ParentKey0)}`,
                       x.{DbCacheEntry.ParentHash1Column} `{nameof(DbCacheEntry.ParentHash1)}`,
                       x.{DbCacheEntry.ParentKey1Column} `{nameof(DbCacheEntry.ParentKey1)}`,
                       x.{DbCacheEntry.ParentHash2Column} `{nameof(DbCacheEntry.ParentHash2)}`,
                       x.{DbCacheEntry.ParentKey2Column} `{nameof(DbCacheEntry.ParentKey2)}`,
                       x.{DbCacheEntry.ParentHash3Column} `{nameof(DbCacheEntry.ParentHash3)}`,
                       x.{DbCacheEntry.ParentKey3Column} `{nameof(DbCacheEntry.ParentKey3)}`,
                       x.{DbCacheEntry.ParentHash4Column} `{nameof(DbCacheEntry.ParentHash4)}`,
                       x.{DbCacheEntry.ParentKey4Column} `{nameof(DbCacheEntry.ParentKey4)}`,
                       x.{DbCacheEntry.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       x.{DbCacheEntry.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`
                  from {CacheSchemaName}.{CacheEntriesTableName} x
                 where x.{DbCacheEntry.HashColumn} = @{nameof(DbCacheEntry.Single.Hash)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or x.{DbCacheEntry.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select y.`{nameof(DbCacheEntry.UtcExpiry)}` `{nameof(DbCacheValue.UtcExpiry)}`,
                       y.`{nameof(DbCacheEntry.Interval)}` `{nameof(DbCacheValue.Interval)}`,
                       y.`{nameof(DbCacheEntry.Value)}` `{nameof(DbCacheValue.Value)}`,
                       y.`{nameof(DbCacheEntry.Compressed)}` `{nameof(DbCacheValue.Compressed)}`
                  from ({PeekCacheEntryQuery}) y
            ");

            GetCacheSizeInBytesQuery = MinifyQuery($@"
                select round(sum(length({DbCacheEntry.ValueColumn})))
                  from {CacheSchemaName}.{CacheEntriesTableName};
            ");

            #endregion Queries
        }
    }
}