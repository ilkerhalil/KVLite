//
// Queries.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace PommaLabs.KVLite.Core
{
    internal static class Queries
    {
        public const string CacheSchema = @"
            PRAGMA auto_vacuum = FULL;
            CREATE TABLE CacheItem (
                partition TEXT NOT NULL,
                key TEXT NOT NULL,
                serializedValue BLOB NOT NULL,
                utcCreation BIGINT NOT NULL,
                utcExpiry BIGINT NOT NULL,
                interval BIGINT,
                CONSTRAINT CacheItem_PK PRIMARY KEY (partition, key)
            );
            CREATE INDEX UtcExpiry_Idx ON CacheItem (utcExpiry ASC);
        ";

        public const string Clear = @"
            delete from CacheItem
             where @ignoreExpirationDate = 1
                or utcExpiry <= strftime('%s', 'now'); -- Clear only invalid rows
        ";

        public const string Contains = @"
            select interval is not null as sliding
              from CacheItem
             where partition = @partition
               and key = @key
               and utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ";

        public const string Count = @"
            select count(*)
              from CacheItem
             where @ignoreExpirationDate = 1
                or utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ";

        public const string DoAdd = @"
            insert or replace into CacheItem (partition, key, serializedValue, utcCreation, utcExpiry, interval)
            values (@partition, @key, @serializedValue, strftime('%s', 'now'), @utcExpiry, @interval);
        ";

        public const string DoGetAllItems = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where interval is not null
               and utcExpiry > strftime('%s', 'now'); -- Update only valid rows
            select *
              from CacheItem
             where utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ";

        public const string DoGetPartitionItems = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where partition = @partition
               and interval is not null
               and utcExpiry > strftime('%s', 'now'); -- Update only valid rows
            select * 
              from CacheItem
             where partition = @partition
               and utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ";

        public const string GetItem = @"
            select *
              from CacheItem
             where partition = @partition
               and key = @key
               and utcExpiry > strftime('%s', 'now'); -- Select only valid rows
        ";

        public const string Remove = @"
            delete from CacheItem
             where partition = @partition
               and key = @key;
        ";

        public const string SchemaIsReady = @"
            select count(*)
              from sqlite_master
             where name = 'CacheItem';
        ";

        public const string SetPragmas = @"
            PRAGMA cache_spill = 1;
            PRAGMA count_changes = 0; /* Not required by our queries */
            PRAGMA journal_size_limit = {0}; /* Size in bytes */
            PRAGMA read_uncommitted = 1; /* Allows multiple readers */
            PRAGMA temp_store = MEMORY;
        ";

        public const string UpdateExpiry = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where partition = @partition
               and key = @key
               and interval is not null
               and utcExpiry > strftime('%s', 'now'); -- Update only valid rows
        ";

        public const string Vacuum = @"vacuum; -- Clears free list and makes DB file smaller";
    }
}
