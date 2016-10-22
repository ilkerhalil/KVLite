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

using MySql.Data.MySqlClient;
using System.Data;

namespace PommaLabs.KVLite.MySql
{
    public class MySqlCacheConnectionFactory : DbCacheConnectionFactory
    {
        public MySqlCacheConnectionFactory()
            : base(MySqlClientFactory.Instance, null, null, null)
        {
            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                insert into {CacheItemsTableName} (
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

                insert into {CacheValuesTableName} (
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

            DeleteCacheEntriesCommand = MinifyQuery($@"
                delete from {CacheItemsTableName}
                 where (@{nameof(DbCacheEntry.Group.Partition)} is null or {DbCacheEntry.PartitionColumn} = @{nameof(DbCacheEntry.Group.Partition)})
                   and (@{nameof(DbCacheEntry.Group.IgnoreExpiryDate)} or {DbCacheEntry.UtcExpiryColumn} < @{nameof(DbCacheEntry.Group.UtcNow)})
            ");
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