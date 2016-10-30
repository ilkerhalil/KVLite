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
using System.Data.Common;
using System.Reflection;

namespace PommaLabs.KVLite.MySql
{
    /// <summary>
    ///   Cache connection factory specialized for MySQL.
    /// </summary>
    public class MySqlCacheConnectionFactory : DbCacheConnectionFactory
    {
        /// <summary>
        ///   Cache connection factory specialized for MySQL.
        /// </summary>
        public MySqlCacheConnectionFactory()
            : base(GetDbProviderFactory(), null, null)
        {
            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert into {CacheSchemaName}.{CacheEntriesTableName} (
                    {DbCacheEntry.PartitionColumn}, 
                    {DbCacheEntry.KeyColumn}, 
                    {DbCacheValue.UtcExpiryColumn}, 
                    {DbCacheValue.IntervalColumn},
                    {DbCacheValue.ValueColumn}, 
                    {DbCacheValue.CompressedColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentKey2Column},
                    {DbCacheEntry.ParentKey3Column},
                    {DbCacheEntry.ParentKey4Column}
                )
                values (
                    @{nameof(DbCacheEntry.Partition)}, 
                    @{nameof(DbCacheEntry.Key)}, 
                    @{nameof(DbCacheEntry.UtcExpiry)}, 
                    @{nameof(DbCacheEntry.Interval)},
                    @{nameof(DbCacheEntry.Value)}, 
                    @{nameof(DbCacheEntry.Compressed)},
                    @{nameof(DbCacheEntry.UtcCreation)},
                    @{nameof(DbCacheEntry.ParentKey0)},
                    @{nameof(DbCacheEntry.ParentKey1)},
                    @{nameof(DbCacheEntry.ParentKey2)},
                    @{nameof(DbCacheEntry.ParentKey3)},
                    @{nameof(DbCacheEntry.ParentKey4)}
                )
                on duplicate key update
                    {DbCacheValue.UtcExpiryColumn} = @{nameof(DbCacheEntry.UtcExpiry)},
                    {DbCacheValue.IntervalColumn} = @{nameof(DbCacheEntry.Interval)},
                    {DbCacheValue.ValueColumn} = @{nameof(DbCacheEntry.Value)},
                    {DbCacheValue.CompressedColumn} = @{nameof(DbCacheEntry.Compressed)},
                    {DbCacheEntry.UtcCreationColumn} = @{nameof(DbCacheEntry.UtcCreation)},
                    {DbCacheEntry.ParentKey0Column} = @{nameof(DbCacheEntry.ParentKey0)},
                    {DbCacheEntry.ParentKey1Column} = @{nameof(DbCacheEntry.ParentKey1)},
                    {DbCacheEntry.ParentKey2Column} = @{nameof(DbCacheEntry.ParentKey2)},
                    {DbCacheEntry.ParentKey3Column} = @{nameof(DbCacheEntry.ParentKey3)},
                    {DbCacheEntry.ParentKey4Column} = @{nameof(DbCacheEntry.ParentKey4)};
            ");

            DeleteCacheEntryCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheEntriesTableName}
                 where {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheEntry.KeyColumn} = @{nameof(DbCacheEntry.Single.Key)}
            ");

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {CacheSchemaName}.{CacheEntriesTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheValue.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            UpdateCacheEntryExpiryCommand = MinifyQuery($@"
                update {CacheSchemaName}.{CacheEntriesTableName}
                   set {DbCacheValue.UtcExpiryColumn} = @{nameof(DbCacheEntry.Single.UtcExpiry)}
                 where {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheEntry.KeyColumn} = @{nameof(DbCacheEntry.Single.Key)}
            ");

            #endregion Commands

            #region Queries

            ContainsCacheEntryQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheEntriesTableName}
                 where {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheEntry.KeyColumn} = @{nameof(DbCacheEntry.Single.Key)}
                   and {DbCacheValue.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)}
            ");

            CountCacheEntriesQuery = MinifyQuery($@"
                select count(*)
                  from {CacheSchemaName}.{CacheEntriesTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheValue.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntriesQuery = MinifyQuery($@"
                select x.{DbCacheEntry.PartitionColumn} `{nameof(DbCacheEntry.Partition)}`,
                       x.{DbCacheEntry.KeyColumn} `{nameof(DbCacheEntry.Key)}`,
                       x.{DbCacheValue.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       x.{DbCacheValue.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       x.{DbCacheValue.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       x.{DbCacheValue.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`,
                       x.{DbCacheEntry.UtcCreationColumn} `{nameof(DbCacheEntry.UtcCreation)}`,
                       x.{DbCacheEntry.ParentKey0Column} `{nameof(DbCacheEntry.ParentKey0)}`,
                       x.{DbCacheEntry.ParentKey1Column} `{nameof(DbCacheEntry.ParentKey1)}`,
                       x.{DbCacheEntry.ParentKey2Column} `{nameof(DbCacheEntry.ParentKey2)}`,
                       x.{DbCacheEntry.ParentKey3Column} `{nameof(DbCacheEntry.ParentKey3)}`,
                       x.{DbCacheEntry.ParentKey4Column} `{nameof(DbCacheEntry.ParentKey4)}`
                  from {CacheSchemaName}.{CacheEntriesTableName} x
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or x.{DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or x.{DbCacheValue.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Group.UtcExpiry)})
            ");

            PeekCacheEntryQuery = MinifyQuery($@"
                select x.{DbCacheEntry.PartitionColumn} `{nameof(DbCacheEntry.Partition)}`,
                       x.{DbCacheEntry.KeyColumn} `{nameof(DbCacheEntry.Key)}`,
                       x.{DbCacheValue.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       x.{DbCacheValue.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       x.{DbCacheValue.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       x.{DbCacheValue.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`,
                       x.{DbCacheEntry.UtcCreationColumn} `{nameof(DbCacheEntry.UtcCreation)}`,
                       x.{DbCacheEntry.ParentKey0Column} `{nameof(DbCacheEntry.ParentKey0)}`,
                       x.{DbCacheEntry.ParentKey1Column} `{nameof(DbCacheEntry.ParentKey1)}`,
                       x.{DbCacheEntry.ParentKey2Column} `{nameof(DbCacheEntry.ParentKey2)}`,
                       x.{DbCacheEntry.ParentKey3Column} `{nameof(DbCacheEntry.ParentKey3)}`,
                       x.{DbCacheEntry.ParentKey4Column} `{nameof(DbCacheEntry.ParentKey4)}`
                  from {CacheSchemaName}.{CacheEntriesTableName} x
                 where {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheEntry.KeyColumn} = @{nameof(DbCacheEntry.Single.Key)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or x.{DbCacheValue.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            PeekCacheValueQuery = MinifyQuery($@"
                select x.{DbCacheValue.UtcExpiryColumn} `{nameof(DbCacheEntry.UtcExpiry)}`,
                       x.{DbCacheValue.IntervalColumn} `{nameof(DbCacheEntry.Interval)}`,
                       x.{DbCacheValue.ValueColumn} `{nameof(DbCacheEntry.Value)}`,
                       x.{DbCacheValue.CompressedColumn} `{nameof(DbCacheEntry.Compressed)}`
                  from {CacheSchemaName}.{CacheEntriesTableName} x
                 where {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Single.Partition)}
                   and {DbCacheEntry.KeyColumn} = @{nameof(DbCacheEntry.Single.Key)}
                   and (@{nameof(DbCacheEntry.Single.IgnoreExpiryDate)} or x.{DbCacheValue.UtcExpiryColumn} >= @{nameof(DbCacheEntry.Single.UtcExpiry)})
            ");

            GetCacheSizeInBytesQuery = MinifyQuery($@"
                select round(sum(length({DbCacheValue.ValueColumn})))
                  from {CacheSchemaName}.{CacheEntriesTableName};
            ");

            #endregion Queries
        }

        private static DbProviderFactory GetDbProviderFactory()
        {
            try
            {
                var factoryType = Type.GetType("MySql.Data.MySqlClient.MySqlClientFactory, MySql.Data");
                return factoryType.GetField("Instance", BindingFlags.Public | BindingFlags.Static).GetValue(null) as DbProviderFactory;
            }
            catch (Exception ex)
            {
                throw new Exception(ErrorMessages.MissingMySqlDriver, ex);
            }
        }
    }
}