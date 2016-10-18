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
    internal static class SQLiteQueries
    {
        #region Queries

        public static readonly string CacheSchema = @"
            PRAGMA auto_vacuum = INCREMENTAL;
            DROP TABLE IF EXISTS kvl_cache_items;
            CREATE TABLE kvl_cache_items (
                kvli_hash BIGINT NOT NULL,
                kvli_partition TEXT NOT NULL,
                kvli_key TEXT NOT NULL,
                kvli_creation BIGINT NOT NULL,
                kvli_expiry BIGINT NOT NULL,
                kvli_interval BIGINT NOT NULL,
                kvli_compressed BOOLEAN NOT NULL,
                kvli_parent_hash0 BIGINT,
                kvli_parent_key0 TEXT,
                kvli_parent_hash1 BIGINT,
                kvli_parent_key1 TEXT,
                kvli_parent_hash2 BIGINT,
                kvli_parent_key2 TEXT,
                kvli_parent_hash3 BIGINT,
                kvli_parent_key3 TEXT,
                kvli_parent_hash4 BIGINT,
                kvli_parent_key4 TEXT,
                kvli_value BLOB NOT NULL,
                CONSTRAINT pk_kvli PRIMARY KEY (kvli_hash),
                CONSTRAINT fk_kvli_parent0 FOREIGN KEY (kvli_parent_hash0) REFERENCES kvl_cache_items (kvli_hash) ON DELETE CASCADE,
                CONSTRAINT fk_kvli_parent1 FOREIGN KEY (kvli_parent_hash1) REFERENCES kvl_cache_items (kvli_hash) ON DELETE CASCADE,
                CONSTRAINT fk_kvli_parent2 FOREIGN KEY (kvli_parent_hash2) REFERENCES kvl_cache_items (kvli_hash) ON DELETE CASCADE,
                CONSTRAINT fk_kvli_parent3 FOREIGN KEY (kvli_parent_hash3) REFERENCES kvl_cache_items (kvli_hash) ON DELETE CASCADE,
                CONSTRAINT fk_kvli_parent4 FOREIGN KEY (kvli_parent_hash4) REFERENCES kvl_cache_items (kvli_hash) ON DELETE CASCADE
            );
            CREATE INDEX ix_kvli_exp_part ON kvl_cache_items (kvli_expiry DESC, kvli_partition ASC);
            CREATE INDEX ix_kvli_parent0 ON kvl_cache_items (kvli_parent_hash0);
            CREATE INDEX ix_kvli_parent1 ON kvl_cache_items (kvli_parent_hash1);
            CREATE INDEX ix_kvli_parent2 ON kvl_cache_items (kvli_parent_hash2);
            CREATE INDEX ix_kvli_parent3 ON kvl_cache_items (kvli_parent_hash3);
            CREATE INDEX ix_kvli_parent4 ON kvl_cache_items (kvli_parent_hash4);
        ";

        public static readonly string IsSchemaReady = @"
            PRAGMA table_info(kvl_cache_items)
        ";

        public static readonly string Vacuum = @"
            vacuum; -- Clears free list and makes DB file smaller
        ";

        #endregion Queries
    }
}