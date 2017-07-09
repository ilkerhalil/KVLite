// File name: MySqlCacheConnectionFactory.cs
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

using MySql.Data.MySqlClient;
using PommaLabs.KVLite.Database;

namespace PommaLabs.KVLite.MySql
{
    /// <summary>
    ///   Cache connection factory specialized for MySQL.
    /// </summary>
    public sealed class MySqlCacheConnectionFactory : DbCacheConnectionFactory<MySqlConnection>
    {
        /// <summary>
        ///   Cache connection factory specialized for MySQL.
        /// </summary>
        public MySqlCacheConnectionFactory()
            : base(MySqlClientFactory.Instance, null, null)
        {
        }

        /// <summary>
        ///   The symbol used to enclose an identifier (left side).
        /// </summary>
        protected override string LeftIdentifierEncloser { get; } = "`";

        /// <summary>
        ///   The symbol used to enclose an identifier (right side).
        /// </summary>
        protected override string RightIdentifierEncloser { get; } = "`";

        /// <summary>
        ///   This method is called when either the cache schema name or the cache entries table name
        ///   have been changed by the user.
        /// </summary>
        protected override void UpdateCommandsAndQueries()
        {
            base.UpdateCommandsAndQueries();

            var p = ParameterPrefix;
            var s = SqlSchemaWithDot;

            #region Commands

            InsertOrUpdateCacheEntryCommand = MinifyQuery($@"
                replace into {s}{CacheEntriesTableName} (
                    {DbCacheValue.HashColumn},
                    {DbCacheValue.UtcExpiryColumn},
                    {DbCacheValue.IntervalColumn},
                    {DbCacheValue.ValueColumn},
                    {DbCacheValue.CompressedColumn},
                    {DbCacheEntry.PartitionColumn},
                    {DbCacheEntry.KeyColumn},
                    {DbCacheEntry.UtcCreationColumn},
                    {DbCacheEntry.ParentHash0Column},
                    {DbCacheEntry.ParentKey0Column},
                    {DbCacheEntry.ParentHash1Column},
                    {DbCacheEntry.ParentKey1Column},
                    {DbCacheEntry.ParentHash2Column},
                    {DbCacheEntry.ParentKey2Column}
                )
                values (
                    {p}{nameof(DbCacheValue.Hash)},
                    {p}{nameof(DbCacheValue.UtcExpiry)},
                    {p}{nameof(DbCacheValue.Interval)},
                    {p}{nameof(DbCacheValue.Value)},
                    {p}{nameof(DbCacheValue.Compressed)},
                    {p}{nameof(DbCacheEntry.Partition)},
                    {p}{nameof(DbCacheEntry.Key)},
                    {p}{nameof(DbCacheEntry.UtcCreation)},
                    {p}{nameof(DbCacheEntry.ParentHash0)},
                    {p}{nameof(DbCacheEntry.ParentKey0)},
                    {p}{nameof(DbCacheEntry.ParentHash1)},
                    {p}{nameof(DbCacheEntry.ParentKey1)},
                    {p}{nameof(DbCacheEntry.ParentHash2)},
                    {p}{nameof(DbCacheEntry.ParentKey2)}
                );
            ");

            #endregion Commands
        }
    }
}
