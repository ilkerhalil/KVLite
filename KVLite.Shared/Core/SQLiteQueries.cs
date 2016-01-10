// File name: SQLiteQueries.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Finsa.CodeServices.Common.Text;
using System.Text.RegularExpressions;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   All queries used inside the <see cref="PersistentCache"/> class.
    /// </summary>
    static class SQLiteQueries
    {
        #region Private Queries

        const string UpdateManyItems = @"
            update CacheItem
               set utcExpiry = @utcNow + interval
             where (@partition is null or partition = @partition)
               and interval != 0
               and utcExpiry > @utcNow; -- Update only valid rows
        ";

        const string UpdateOneItem = @"
            update CacheItem
               set utcExpiry = @utcNow + interval
             where partition = @partition
               and key = @key
               and interval != 0
               and utcExpiry > @utcNow; -- Update only valid rows
        ";

        #endregion Private Queries

        #region Public Queries

        public static readonly string CacheSchema = MinifyQuery(@"
            PRAGMA auto_vacuum = FULL;
            DROP TABLE IF EXISTS CacheItem;
            CREATE TABLE CacheItem (
                partition TEXT NOT NULL,
                key TEXT NOT NULL,
                serializedValue BLOB NOT NULL,
                utcCreation BIGINT NOT NULL,
                utcExpiry BIGINT NOT NULL,
                interval BIGINT NOT NULL,
                parentKey0 TEXT,
                parentKey1 TEXT,
                parentKey2 TEXT,
                parentKey3 TEXT,
                parentKey4 TEXT,
                parentKey5 TEXT,
                parentKey6 TEXT,
                parentKey7 TEXT,
                parentKey8 TEXT,
                parentKey9 TEXT,
                CONSTRAINT CacheItem_PK PRIMARY KEY (partition, key),
                CONSTRAINT CacheItem_FK0 FOREIGN KEY (partition, parentKey0) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK1 FOREIGN KEY (partition, parentKey1) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK2 FOREIGN KEY (partition, parentKey2) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK3 FOREIGN KEY (partition, parentKey3) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK4 FOREIGN KEY (partition, parentKey4) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK5 FOREIGN KEY (partition, parentKey5) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK6 FOREIGN KEY (partition, parentKey6) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK7 FOREIGN KEY (partition, parentKey7) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK8 FOREIGN KEY (partition, parentKey8) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK9 FOREIGN KEY (partition, parentKey9) REFERENCES CacheItem (partition, key) ON DELETE CASCADE
            );
            CREATE INDEX CacheItem_UtcExpiry_Idx ON CacheItem (utcExpiry ASC);
            CREATE INDEX CacheItem_ParentKey0_Idx ON CacheItem (partition, parentKey0);
            CREATE INDEX CacheItem_ParentKey1_Idx ON CacheItem (partition, parentKey1);
            CREATE INDEX CacheItem_ParentKey2_Idx ON CacheItem (partition, parentKey2);
            CREATE INDEX CacheItem_ParentKey3_Idx ON CacheItem (partition, parentKey3);
            CREATE INDEX CacheItem_ParentKey4_Idx ON CacheItem (partition, parentKey4);
            CREATE INDEX CacheItem_ParentKey5_Idx ON CacheItem (partition, parentKey5);
            CREATE INDEX CacheItem_ParentKey6_Idx ON CacheItem (partition, parentKey6);
            CREATE INDEX CacheItem_ParentKey7_Idx ON CacheItem (partition, parentKey7);
            CREATE INDEX CacheItem_ParentKey8_Idx ON CacheItem (partition, parentKey8);
            CREATE INDEX CacheItem_ParentKey9_Idx ON CacheItem (partition, parentKey9);
        ");

        public static readonly string Add = MinifyQuery(@"
            insert or ignore into CacheItem (partition, key, serializedValue, utcCreation, utcExpiry, interval, 
                                             parentKey0, parentKey1, parentKey2, parentKey3, parentKey4, 
                                             parentKey5, parentKey6, parentKey7, parentKey8, parentKey9)
            values (@partition, @key, @serializedValue, @utcNow, @utcExpiry, @interval, 
                    @parentKey0, @parentKey1, @parentKey2, @parentKey3, @parentKey4, 
                    @parentKey5, @parentKey6, @parentKey7, @parentKey8, @parentKey9);

            update CacheItem
               set serializedValue = @serializedValue, utcCreation = @utcNow, utcExpiry = @utcExpiry, interval = @interval,
                   parentKey0 = @parentKey0, parentKey1 = @parentKey1, parentKey2 = @parentKey2, parentKey3 = @parentKey3,
                   parentKey4 = @parentKey4, parentKey5 = @parentKey5, parentKey6 = @parentKey6, parentKey7 = @parentKey7,
                   parentKey8 = @parentKey8, parentKey9 = @parentKey9
             where changes() = 0 -- Above INSERT has failed
               and partition = @partition and key = @key

            insert or replace into CacheItem (partition, key, serializedValue, utcCreation, utcExpiry, interval)
            values ('KVLite.CacheVariables', 'insertion_count', 
                    (select coalesce(max(serializedValue) + 1, 1) as insertion_count 
                       from CacheItem 
                      where partition = {CacheVariablesPartition} 
                        and key = {InsertionCountVariable}
                        and utcExpiry > @utcNow
                        and serializedValue < @maxInsertionCount), -- Set to 1 when reaching the limit
                    @utcNow, @utcNow + {CacheVariablesIntervalInSeconds}, {CacheVariablesIntervalInSeconds});

            select coalesce(max(serializedValue), 0) as insertion_count 
              from CacheItem 
             where rowid = last_insert_rowid()
        ");

        public static readonly string Clear = MinifyQuery(@"
            delete from CacheItem
             where (@partition is null or partition = @partition)
               and ((@partition is null and partition = {CacheVariablesPartition}) -- Clear cache variables
                    or @ignoreExpiryDate = 1 or utcExpiry <= @utcNow) -- Clear only invalid rows
        ");

        public static readonly string Count = MinifyQuery(@"
            select count(*)
              from CacheItem
             where (@partition is null or partition = @partition)
               and partition != {CacheVariablesPartition} -- Ignore cache variables
               and (@ignoreExpiryDate = 1 or utcExpiry > @utcNow) -- Select only valid rows
        ");

        public static readonly string IsSchemaReady = MinifyQuery(@"
            PRAGMA table_info(CacheItem)
        ");

        public static readonly string PeekManyItems = MinifyQuery(@"
            select partition, key, serializedValue, utcCreation, utcExpiry, interval, 
                   parentKey0, parentKey1, parentKey2, parentKey3, parentKey4, 
                   parentKey5, parentKey6, parentKey7, parentKey8, parentKey9
              from CacheItem
             where (@partition is null or partition = @partition)
               and partition != {CacheVariablesPartition} -- Ignore cache variables
               and utcExpiry > @utcNow -- Select only valid rows
        ");

        public static readonly string PeekOneItem = MinifyQuery(@"
            select partition, key, serializedValue, utcCreation, utcExpiry, interval, 
                   parentKey0, parentKey1, parentKey2, parentKey3, parentKey4, 
                   parentKey5, parentKey6, parentKey7, parentKey8, parentKey9
              from CacheItem
             where partition = @partition
               and key = @key
               and utcExpiry > @utcNow -- Select only valid rows
        ");

        public static readonly string PeekOne = MinifyQuery(@"select serializedValue from (" + PeekOneItem + ")");

        public static readonly string GetManyItems = MinifyQuery(UpdateManyItems + PeekManyItems);

        public static readonly string GetOneItem = MinifyQuery(UpdateOneItem + PeekOneItem);

        public static readonly string GetOne = MinifyQuery(UpdateOneItem + PeekOne);

        public static readonly string Contains = MinifyQuery(@"select count(*) from (" + PeekOneItem + ")");

        public static readonly string Remove = MinifyQuery(@"
            delete from CacheItem
             where partition = @partition
               and key = @key
        ");

        public static readonly string SetPragmas = MinifyQuery(@"
            PRAGMA foreign_keys = ON;
            PRAGMA cache_spill = 1;
            PRAGMA count_changes = 1; -- Required by INSERT/UPDATE
            PRAGMA journal_size_limit = {0}; -- Size in bytes
            PRAGMA wal_autocheckpoint = {1};
            PRAGMA temp_store = MEMORY;
        ");

        public static readonly string Vacuum = MinifyQuery(@"
            vacuum; -- Clears free list and makes DB file smaller
        ");

        #endregion Public Queries

        #region Private Methods

        static string MinifyQuery(string query)
        {
            // Removes all SQL comments. Multiline excludes '/n' from '.' matches.
            query = Regex.Replace(query, @"--.*", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled);

            // Removes all multiple blanks.
            query = Regex.Replace(query, @"\s+", " ", RegexOptions.Compiled);

            // Removes query placeholders.
            query = new FastReplacer("{", "}")
                .Append(query)
                .Replace("{CacheVariablesPartition}", "'KVLite.CacheVariables'")
                .Replace("{InsertionCountVariable}", "'insertion_count'")
                .Replace("{CacheVariablesIntervalInSeconds}", "3600000") // 1000 hours                
                .ToString();

            // Removes initial and ending blanks.
            return query.Trim();
        }

        #endregion Private Methods
    }
}