// File name: Queries.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Text.RegularExpressions;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   All queries used inside the <see cref="PersistentCache"/> class.
    /// </summary>
    internal static class SQLiteQueries
    {
        #region Queries

        public static readonly string CacheSchema = MinifyQuery(@"
            PRAGMA auto_vacuum = FULL;
            CREATE TABLE CacheItem (
                partition TEXT NOT NULL,
                key TEXT NOT NULL,
                utcCreation BIGINT NOT NULL,
                utcExpiry BIGINT NOT NULL,
                interval BIGINT,
                serializedValue BLOB NOT NULL,
                static readonlyRAINT CacheItem_PK PRIMARY KEY (partition, key)
            );
            CREATE INDEX UtcExpiry_Idx ON CacheItem (utcExpiry ASC);
        ");

        public static readonly string Add = MinifyQuery(@"
            insert or replace into CacheItem (partition, key, serializedValue, utcCreation, utcExpiry, interval)
            values (@partition, @key, @serializedValue, strftime('%s', 'now'), @utcExpiry, @interval);
        ");

        public static readonly string Clear = MinifyQuery(@"
            delete from CacheItem
             where @ignoreExpirationDate = 1
                or utcExpiry <= strftime('%s', 'now'); -- Clear only invalid rows
        ");

        public static readonly string Count = MinifyQuery(@"
            select count(*)
              from CacheItem
             where @ignoreExpirationDate = 1
                or utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ");

        public static readonly string Get = MinifyQuery(@"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where (@partition is null or partition = @partition)
               and (@key is null or key = @key)
               and interval is not null
               and utcExpiry > strftime('%s', 'now'); -- Update only valid rows
            select *
              from CacheItem
             where (@partition is null or partition = @partition)
               and (@key is null or key = @key)
               and utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ");

        public static readonly string IsSchemaReady = MinifyQuery(@"
            select count(*)
              from sqlite_master
             where name = 'CacheItem';
        ");

        public static readonly string Peek = MinifyQuery(@"
            select *
              from CacheItem
             where (@partition is null or partition = @partition)
               and (@key is null or key = @key)
               and utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ");

        public static readonly string Remove = MinifyQuery(@"
            delete from CacheItem
             where partition = @partition
               and key = @key;
        ");

        public static readonly string SetPragmas = MinifyQuery(@"
            PRAGMA cache_spill = 1;
            PRAGMA count_changes = 0; /* Not required by our queries */
            PRAGMA journal_size_limit = {0}; /* Size in bytes */
            PRAGMA wal_autocheckpoint = {1};
            PRAGMA temp_store = MEMORY;
        ");

        public static readonly string Vacuum = MinifyQuery(@"
            vacuum; -- Clears free list and makes DB file smaller
        ");

        #endregion Queries

        #region Private Methods

        private static string MinifyQuery(string query)
        {
            return Regex.Replace(query, @"\s+", " ");
        }

        #endregion Private Methods
    }
}