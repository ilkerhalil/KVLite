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

using CodeProject.ObjectPool;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SQLite;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;

namespace PommaLabs.KVLite.SQLite
{
    public sealed class SQLiteCacheConnectionFactory
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

        /// <summary>
        ///   The connection pool used to cache open connections.
        /// </summary>
        private ObjectPool<PooledConnection> _connectionPool;

        public SQLiteCacheConnectionFactory()
        {
            // Connection string must be customized by each cache.
            InitConnectionString();

            using (var db = _connectionPool.GetObject())
            using (var cmd = db.Connection.CreateCommand())
            {
                bool isSchemaReady;
                cmd.CommandText = SQLiteQueries.IsSchemaReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsSchemaReady(dataReader);
                }
                if (!isSchemaReady)
                {
                    // Creates the ICacheItem table and the required indexes.
                    cmd.CommandText = SQLiteQueries.CacheSchema;
                    cmd.ExecuteNonQuery();
                }
            }
        }

        #region Settings

        private int _maxCacheSizeInMB;
        private int _maxJournalSizeInMB;

        /// <summary>
        ///   Max size in megabytes for the cache.
        /// </summary>
        public int MaxCacheSizeInMB
        {
            get
            {
                var result = _maxCacheSizeInMB;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentOutOfRangeException.If(value <= 0);

                _maxCacheSizeInMB = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Max size in megabytes for the SQLite journal log.
        /// </summary>
        public int MaxJournalSizeInMB
        {
            get
            {
                var result = _maxJournalSizeInMB;

                // Postconditions
                Debug.Assert(result > 0);
                return result;
            }
            set
            {
                // Preconditions
                Raise.ArgumentOutOfRangeException.If(value <= 0);

                _maxJournalSizeInMB = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public IDataProvider DataProvider { get; } = new SQLiteDataProvider();

        public long GetCacheSizeInKB()
        {
            // No need for a transaction, since it is just a select.
            using (var db = _connectionPool.GetObject())
            using (var cmd = db.Connection.CreateCommand())
            {
                cmd.CommandText = "PRAGMA page_count;";
                var pageCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA freelist_count;";
                var freelistCount = (long) cmd.ExecuteScalar();

                cmd.CommandText = "PRAGMA page_size;";
                var pageSizeInKB = (long) cmd.ExecuteScalar() / 1024L;

                return (pageCount - freelistCount) * pageSizeInKB;
            }
        }

        /// <summary>
        ///   Runs VACUUM on the underlying SQLite database.
        /// </summary>
        public void Vacuum()
        {
            try
            {
                _log.Info($"Vacuuming the SQLite DB '{Settings.CacheUri}'...");

                // Vacuum cannot be run within a transaction.
                using (var db = _connectionPool.GetObject())
                using (var cmd = db.Connection.CreateCommand())
                {
                    cmd.CommandText = SQLiteQueries.Vacuum;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnVacuum, ex);
            }
        }

        private PooledConnection CreateDbInterface()
        {
#pragma warning disable CC0022 // Should dispose object

            // Create and open the connection.
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();

#pragma warning restore CC0022 // Should dispose object

            // Sets PRAGMAs for this new connection.
            using (var cmd = connection.CreateCommand())
            {
                var journalSizeLimitInBytes = MaxJournalSizeInMB * 1024 * 1024;
                cmd.CommandText = string.Format(SQLiteQueries.SetPragmas, journalSizeLimitInBytes);
                cmd.ExecuteNonQuery();
            }

#pragma warning disable CC0022 // Should dispose object

            return new PooledConnection(connection);

#pragma warning restore CC0022 // Should dispose object
        }

        private void InitConnectionString()
        {
            SQLiteJournalModeEnum journalMode;
            var cacheUri = GetDataSource(out journalMode);

            var builder = new SQLiteConnectionStringBuilder
            {
                BaseSchemaName = "kvlite",
                FullUri = cacheUri,
                JournalMode = journalMode,
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
                Enlist = false,
                DefaultIsolationLevel = System.Data.IsolationLevel.ReadCommitted,

                /* Required by parent keys */
                ForeignKeys = true,
                RecursiveTriggers = true,

                /* Each page is 4KB large - Multiply by 1024*1024/PageSizeInBytes */
                MaxPageCount = MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes,
                PageSize = PageSizeInBytes,
                CacheSize = -2000,

                /* We use a custom object pool */
                Pooling = false,
            };

            _connectionString = builder.ToString();
            _connectionPool = new ObjectPool<PooledConnection>(1, 10, CreateDbInterface);
        }

        private static bool IsSchemaReady(SQLiteDataReader dataReader)
        {
            var columns = new HashSet<string>();

            while (dataReader.Read())
            {
                columns.Add(dataReader.GetValue(dataReader.GetOrdinal("name")) as string);
            }

            return columns.Count == 11
                && columns.Contains("partition")
                && columns.Contains("key")
                && columns.Contains("serializedValue")
                && columns.Contains("utcCreation")
                && columns.Contains("utcExpiry")
                && columns.Contains("interval")
                && columns.Contains("parentKey0")
                && columns.Contains("parentKey1")
                && columns.Contains("parentKey2")
                && columns.Contains("parentKey3")
                && columns.Contains("parentKey4");
        }

        #region Nested type: PooledConnection

        private sealed class PooledConnection : PooledObject
        {
            public PooledConnection(SQLiteConnection connection)
            {
                Connection = connection;
            }

            protected override void OnReleaseResources()
            {
                Connection.Dispose();
                base.OnReleaseResources();
            }

            public SQLiteConnection Connection { get; }
        }

        #endregion Nested type: PooledConnection 
    }
}
