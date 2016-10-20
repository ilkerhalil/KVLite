// File name: SQLiteDbCacheConnectionFactory.cs
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

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;

namespace PommaLabs.KVLite.SQLite
{
    internal sealed class SQLiteCacheConnectionFactory<TSettings> : DbCacheConnectionFactory
        where TSettings : SQLiteCacheSettings<TSettings>
    {
        #region Constants

        /// <summary>
        ///   The default SQLite page size in bytes. Do not change this value unless SQLite changes
        ///   its defaults. WAL journal does limit the capability to change that value even when the
        ///   DB is still empty.
        /// </summary>
        private const int PageSizeInBytes = 4096;

        #endregion Constants

        /// <summary>
        ///   The connection string used to connect to the SQLite database.
        /// </summary>
        private string _connectionString;

        private readonly TSettings _settings;

        private readonly SQLiteJournalModeEnum _journalMode;

        public SQLiteCacheConnectionFactory(TSettings settings, SQLiteJournalModeEnum journalMode)
            : base(SQLiteFactory.Instance, null, null)
        {
            _settings = settings;
            _journalMode = journalMode;
        }

        #region IDbCacheConnectionFactory members

        /// <summary>
        ///   The connection string used to connect to the cache data provider.
        /// </summary>
        public override string ConnectionString
        {
            get { return _connectionString; }
            set { throw new NotSupportedException(); }
        }

        public override long GetCacheSizeInBytes()
        {
            // No need for a transaction, since it is just a select.
            using (var conn = Create())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = "PRAGMA page_count;";
                var pageCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA freelist_count;";
                var freelistCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA page_size;";
                var pageSizeInKB = (long) cmd.ExecuteScalar();

                return (pageCount - freelistCount) * pageSizeInKB * 1024;
            }
        }

        #endregion IDbCacheConnectionFactory members

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            // Vacuum cannot be run within a transaction.
            using (var conn = Create())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                cmd.CommandText = SQLiteQueries.Vacuum;
                cmd.ExecuteNonQuery();
            }
        }

        internal void InitConnectionString(string dataSource)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = CacheSchemaName,
                FullUri = dataSource,
                JournalMode = _journalMode,
                FailIfMissing = false,
                LegacyFormat = false,
                ReadOnly = false,
                SyncMode = SynchronizationModes.Off,
                Version = 3,

                /* KVLite uses UNIX time */
                DateTimeFormat = SQLiteDateFormats.Ticks,
                DateTimeKind = DateTimeKind.Utc,

                /* Settings three minutes as timeout should be more than enough... */
                DefaultTimeout = 180,
                PrepareRetries = 3,

                /* Transaction handling */
                Enlist = true,
                DefaultIsolationLevel = IsolationLevel.ReadCommitted,

                /* Required by parent keys */
                ForeignKeys = true,
                RecursiveTriggers = true,

                /* Each page is 4KB large - Multiply by 1024*1024/PageSizeInBytes */
                MaxPageCount = _settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes,
                PageSize = PageSizeInBytes,
                CacheSize = -2000,

                /* We need pooling to improve performance */
                Pooling = true,
            };

            _connectionString = builder.ToString();
        }

        internal void EnsureSchemaIsReady()
        {
            using (var conn = Create())
            using (var cmd = conn.CreateCommand())
            {
                conn.Open();

                bool isSchemaReady;
                cmd.CommandText = SQLiteQueries.IsSchemaReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsSchemaReady(dataReader);
                }

                if (!isSchemaReady)
                {
                    // Creates the cache items table and the required indexes.
                    cmd.CommandText = SQLiteQueries.CacheSchema;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static bool IsSchemaReady(IDataReader dataReader)
        {
            var columns = new HashSet<string>();

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 18
                && columns.Contains("kvli_hash")
                && columns.Contains("kvli_partition")
                && columns.Contains("kvli_key")
                && columns.Contains("kvli_creation")
                && columns.Contains("kvli_expiry")
                && columns.Contains("kvli_interval")
                && columns.Contains("kvli_compressed")
                && columns.Contains("kvli_parent_hash0")
                && columns.Contains("kvli_parent_key0")
                && columns.Contains("kvli_parent_hash1")
                && columns.Contains("kvli_parent_key1")
                && columns.Contains("kvli_parent_hash2")
                && columns.Contains("kvli_parent_key2")
                && columns.Contains("kvli_parent_hash3")
                && columns.Contains("kvli_parent_key3")
                && columns.Contains("kvli_parent_hash4")
                && columns.Contains("kvli_parent_key4")
                && columns.Contains("kvli_value");
        }
    }
}