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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.Text.RegularExpressions;

namespace PommaLabs.KVLite.SQLite
{
    /// <summary>
    ///   All queries used inside the <see cref="PersistentCache"/> class.
    /// </summary>
    internal static class SQLiteQueries
    {
        #region Queries

        public static readonly string CacheSchema = MinifyQuery(@"
            PRAGMA auto_vacuum = INCREMENTAL;
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
                CONSTRAINT CacheItem_PK PRIMARY KEY (partition, key),
                CONSTRAINT CacheItem_FK0 FOREIGN KEY (partition, parentKey0) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK1 FOREIGN KEY (partition, parentKey1) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK2 FOREIGN KEY (partition, parentKey2) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK3 FOREIGN KEY (partition, parentKey3) REFERENCES CacheItem (partition, key) ON DELETE CASCADE,
                CONSTRAINT CacheItem_FK4 FOREIGN KEY (partition, parentKey4) REFERENCES CacheItem (partition, key) ON DELETE CASCADE
            );
            CREATE INDEX CacheItem_UtcExpiry_Idx ON CacheItem (utcExpiry ASC);
            CREATE INDEX CacheItem_ParentKey0_Idx ON CacheItem (partition, parentKey0);
            CREATE INDEX CacheItem_ParentKey1_Idx ON CacheItem (partition, parentKey1);
            CREATE INDEX CacheItem_ParentKey2_Idx ON CacheItem (partition, parentKey2);
            CREATE INDEX CacheItem_ParentKey3_Idx ON CacheItem (partition, parentKey3);
            CREATE INDEX CacheItem_ParentKey4_Idx ON CacheItem (partition, parentKey4);
        ");

        public static readonly string IsSchemaReady = MinifyQuery(@"
            PRAGMA table_info(CacheItem)
        ");

        public static readonly string SetPragmas = MinifyQuery(@"
            PRAGMA journal_size_limit = {0}; -- Size in bytes
            PRAGMA temp_store = MEMORY;
        ");

        public static readonly string Vacuum = MinifyQuery(@"
            vacuum; -- Clears free list and makes DB file smaller
        ");

        #endregion Queries

        #region Private Methods

        private static string MinifyQuery(string query)
        {
            // Removes all SQL comments. Multiline excludes '/n' from '.' matches.
            query = Regex.Replace(query, @"--.*", string.Empty, RegexOptions.Multiline | RegexOptions.Compiled);

            // Removes all multiple blanks.
            query = Regex.Replace(query, @"\s+", " ", RegexOptions.Compiled);

            // Removes initial and ending blanks.
            return query.Trim();
        }

        #endregion Private Methods
    }
}