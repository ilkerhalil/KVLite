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

namespace PommaLabs.KVLite.SQLite
{
    /// <summary>
    ///   All queries used inside the <see cref="PersistentCache"/> and <see cref="VolatileCache"/> classes.
    /// </summary>
    public static class SQLiteQueries
    {
        #region Queries

        public static readonly string CacheSchema = @"        
            DROP TABLE IF EXISTS kvl_cache_entries;
            CREATE TABLE kvl_cache_entries (
                kvle_partition TEXT NOT NULL,
                kvle_key TEXT NOT NULL,
                kvle_expiry BIGINT NOT NULL,
                kvle_interval BIGINT NOT NULL,
                kvle_value BLOB NOT NULL,
                kvle_compressed BOOLEAN NOT NULL,
                kvle_creation BIGINT NOT NULL,
                kvle_parent_key0 TEXT,
                kvle_parent_key1 TEXT,
                kvle_parent_key2 TEXT,
                kvle_parent_key3 TEXT,
                kvle_parent_key4 TEXT,
                CONSTRAINT pk_kvle PRIMARY KEY (kvle_partition, kvle_key),
                CONSTRAINT fk_kvle_parent0 FOREIGN KEY (kvle_partition, kvle_parent_key0) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvle_parent1 FOREIGN KEY (kvle_partition, kvle_parent_key1) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvle_parent2 FOREIGN KEY (kvle_partition, kvle_parent_key2) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvle_parent3 FOREIGN KEY (kvle_partition, kvle_parent_key3) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE,
                CONSTRAINT fk_kvle_parent4 FOREIGN KEY (kvle_partition, kvle_parent_key4) REFERENCES kvl_cache_entries (kvle_partition, kvle_key) ON DELETE CASCADE
            );
            CREATE INDEX ix_kvle_exp_part ON kvl_cache_entries (kvle_expiry DESC, kvle_partition ASC);
            CREATE INDEX ix_kvle_parent0 ON kvl_cache_entries (kvle_partition, kvle_parent_key0);
            CREATE INDEX ix_kvle_parent1 ON kvl_cache_entries (kvle_partition, kvle_parent_key1);
            CREATE INDEX ix_kvle_parent2 ON kvl_cache_entries (kvle_partition, kvle_parent_key2);
            CREATE INDEX ix_kvle_parent3 ON kvl_cache_entries (kvle_partition, kvle_parent_key3);
            CREATE INDEX ix_kvle_parent4 ON kvl_cache_entries (kvle_partition, kvle_parent_key4);
        ";

        public static readonly string IsCacheEntriesTableReady = @"
            PRAGMA table_info(kvl_cache_entries)
        ";

        public static readonly string Vacuum = @"
            vacuum; -- Clears free list and makes DB file smaller
        ";

        #endregion Queries
    }
}