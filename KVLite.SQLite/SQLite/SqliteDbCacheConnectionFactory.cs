using CodeProject.ObjectPool;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Text;

namespace PommaLabs.KVLite.SQLite
{
    public sealed class SqliteDbCacheConnectionFactory
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
        private ObjectPool<DbInterface> _connectionPool;

        public SqliteDbCacheConnectionFactory()
        {
            // Connection string must be customized by each cache.
            InitConnectionString();

            using (var db = _connectionPool.GetObject())
            using (var cmd = db.Connection.CreateCommand())
            {
                bool isSchemaReady;
                cmd.CommandText = SqliteQueries.IsSchemaReady;
                using (var dataReader = cmd.ExecuteReader())
                {
                    isSchemaReady = IsSchemaReady(dataReader);
                }
                if (!isSchemaReady)
                {
                    // Creates the ICacheItem table and the required indexes.
                    cmd.CommandText = SqliteQueries.CacheSchema;
                    cmd.ExecuteNonQuery();
                }
            }
        }

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
                    cmd.CommandText = SqliteQueries.Vacuum;
                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                LastError = ex;
                Log.Error(ErrorMessages.InternalErrorOnVacuum, ex);
            }
        }

        private DbInterface CreateDbInterface()
        {
#pragma warning disable CC0022 // Should dispose object

            // Create and open the connection.
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();

#pragma warning restore CC0022 // Should dispose object

            // Sets PRAGMAs for this new connection.
            using (var cmd = connection.CreateCommand())
            {
                var journalSizeLimitInBytes = Settings.MaxJournalSizeInMB * 1024 * 1024;
                cmd.CommandText = string.Format(SqliteQueries.SetPragmas, journalSizeLimitInBytes);
                cmd.ExecuteNonQuery();
            }

#pragma warning disable CC0022 // Should dispose object

            return new DbInterface(connection);

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
                MaxPageCount = Settings.MaxCacheSizeInMB * 1024 * 1024 / PageSizeInBytes,
                PageSize = PageSizeInBytes,
                CacheSize = -2000,

                /* We use a custom object pool */
                Pooling = false,
            };

            _connectionString = builder.ToString();
            _connectionPool = new ObjectPool<DbInterface>(1, 10, CreateDbInterface);
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

        #region Nested type: DbInterface

        private sealed class DbInterface : PooledObject
        {
            public DbInterface(SQLiteConnection connection)
            {
                Connection = connection;

                // Add
                Add_Command = connection.CreateCommand();
                Add_Command.CommandText = SqliteQueries.Add;
                Add_Command.Parameters.Add(Add_Partition = new SQLiteParameter("partition"));
                Add_Command.Parameters.Add(Add_Key = new SQLiteParameter("key"));
                Add_Command.Parameters.Add(Add_SerializedValue = new SQLiteParameter("serializedValue"));
                Add_Command.Parameters.Add(Add_UtcExpiry = new SQLiteParameter("utcExpiry"));
                Add_Command.Parameters.Add(Add_Interval = new SQLiteParameter("interval"));
                Add_Command.Parameters.Add(Add_UtcNow = new SQLiteParameter("utcNow"));
                Add_Command.Parameters.Add(Add_MaxInsertionCount = new SQLiteParameter("maxInsertionCount"));
                Add_Command.Parameters.Add(Add_ParentKey0 = new SQLiteParameter("parentKey0"));
                Add_Command.Parameters.Add(Add_ParentKey1 = new SQLiteParameter("parentKey1"));
                Add_Command.Parameters.Add(Add_ParentKey2 = new SQLiteParameter("parentKey2"));
                Add_Command.Parameters.Add(Add_ParentKey3 = new SQLiteParameter("parentKey3"));
                Add_Command.Parameters.Add(Add_ParentKey4 = new SQLiteParameter("parentKey4"));

                // Vacuum
                IncrementalVacuum_Command = Connection.CreateCommand();
                IncrementalVacuum_Command.CommandText = SqliteQueries.IncrementalVacuum;
            }

            protected override void OnReleaseResources()
            {
                Add_Command.Dispose();
                IncrementalVacuum_Command.Dispose();
                Connection.Dispose();

                base.OnReleaseResources();
            }

            public SQLiteConnection Connection { get; }

            #region Add

            public SQLiteCommand Add_Command { get; }
            public SQLiteParameter Add_Partition { get; }
            public SQLiteParameter Add_Key { get; }
            public SQLiteParameter Add_SerializedValue { get; }
            public SQLiteParameter Add_UtcExpiry { get; }
            public SQLiteParameter Add_Interval { get; }
            public SQLiteParameter Add_UtcNow { get; }
            public SQLiteParameter Add_MaxInsertionCount { get; }
            public SQLiteParameter Add_ParentKey0 { get; }
            public SQLiteParameter Add_ParentKey1 { get; }
            public SQLiteParameter Add_ParentKey2 { get; }
            public SQLiteParameter Add_ParentKey3 { get; }
            public SQLiteParameter Add_ParentKey4 { get; }

            #endregion Add

            #region Vacuum

            public SQLiteCommand IncrementalVacuum_Command { get; }

            #endregion Vacuum
        }

        #endregion Nested type: DbInterface 
    }
}
