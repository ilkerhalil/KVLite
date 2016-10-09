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
            DROP TABLE IF EXISTS kvl_cache_items;
            CREATE TABLE kvl_cache_items (
                kvli_partition TEXT NOT NULL,
                kvli_key TEXT NOT NULL,
                kvli_value BLOB NOT NULL,
                kvli_creation BIGINT NOT NULL,
                kvli_expiry BIGINT NOT NULL,
                kvli_interval BIGINT NOT NULL,
                kvli_parent0 TEXT,
                kvli_parent1 TEXT,
                kvli_parent2 TEXT,
                kvli_parent3 TEXT,
                kvli_parent4 TEXT,
                CONSTRAINT pk_kvl_cache_items PRIMARY KEY (kvli_partition, kvli_key),
                CONSTRAINT fk_kvl_cache_items_parent0 FOREIGN KEY (kvli_partition, kvli_parent0) REFERENCES kvl_cache_items (kvli_partition, kvli_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvl_cache_items_parent1 FOREIGN KEY (kvli_partition, kvli_parent1) REFERENCES kvl_cache_items (kvli_partition, kvli_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvl_cache_items_parent2 FOREIGN KEY (kvli_partition, kvli_parent2) REFERENCES kvl_cache_items (kvli_partition, kvli_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvl_cache_items_parent3 FOREIGN KEY (kvli_partition, kvli_parent3) REFERENCES kvl_cache_items (kvli_partition, kvli_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvl_cache_items_parent4 FOREIGN KEY (kvli_partition, kvli_parent4) REFERENCES kvl_cache_items (kvli_partition, kvli_key) ON DELETE CASCADE
            );
            CREATE INDEX ix_kvl_cache_items_expiry_partition ON kvl_cache_items (kvli_expiry ASC, kvli_partition ASC);
            CREATE INDEX ix_kvl_cache_items_parent0 ON kvl_cache_items (kvli_partition, kvli_parent0);
            CREATE INDEX ix_kvl_cache_items_parent1 ON kvl_cache_items (kvli_partition, kvli_parent1);
            CREATE INDEX ix_kvl_cache_items_parent2 ON kvl_cache_items (kvli_partition, kvli_parent2);
            CREATE INDEX ix_kvl_cache_items_parent3 ON kvl_cache_items (kvli_partition, kvli_parent3);
            CREATE INDEX ix_kvl_cache_items_parent4 ON kvl_cache_items (kvli_partition, kvli_parent4);
        ");

        public static readonly string IsSchemaReady = MinifyQuery(@"
            PRAGMA table_info(kvl_cache_items)
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