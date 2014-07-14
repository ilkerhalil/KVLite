using System;
using System.Data.SqlServerCe;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Web.Caching;
using System.Web.Security;
using DbExtensions;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    public sealed class FileCache : OutputCacheProvider
    {
        private readonly string _cachePath;
        private readonly string _connectionString;

        public FileCache() : this(Configuration.Instance.CachePath ?? Settings.Default.DefaultCachePath)
        {
           // Empty, for now...
        }

        public FileCache(string cachePath)
        {
            Raise<ArgumentException>.IfIsEmpty(cachePath, ErrorMessages.NullOrEmptyCachePath);
            
           var context = HttpContext.Current;
            if (context != null)
            {
                cachePath = context.Server.MapPath(cachePath);
            }

            _cachePath = cachePath;
            _connectionString = CreateConnectionString(cachePath);

            if (!File.Exists(cachePath))
            {
                var engine = new SqlCeEngine(_connectionString);
                engine.CreateDatabase();
            }

            using (var ctx = CacheContext.Create(_connectionString))
            using (ctx.Transaction = ctx.Connection.BeginTransaction())
            {
                try
                {
                   var query = SQL
                      .SELECT("1")
                      .FROM("INFORMATION_SCHEMA.TABLES")
                      .WHERE("TABLE_NAME = {0}", "Cache_Item");
                    
                    var cacheReady = ctx.Exists(query);
                    if (!cacheReady)
                    {
                        ctx.Execute(Settings.Default.CacheCreationScript);
                    }
                    ctx.Transaction.Commit();
                }
                catch
                {
                    ctx.Transaction.Rollback();
                    throw;
                }
            }
        }

        #region Public Properties

        private string CachePath
        {
            get { return _cachePath; }
        }

        public object this[string cacheKey]
        {
            get { return Get(cacheKey); }

            set { Add(cacheKey, value, DateTime.Now.AddDays(365)); }
        }

        #endregion

        public object AddSliding<TObj>(string partition, string key, TObj value, TimeSpan interval)
        {
            return DoAdd(partition, key, value, DateTime.UtcNow, interval);
        }

        public object AddPersistent<TObj>(string partition, string key, TObj value)
        {
            return DoAdd(partition, key, value, null, null);
        }

        public object AddPersistent<TObj>(string key, TObj value)
        {
            return AddPersistent(Settings.Default.DefaultPartition, key, value);
        }

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            return DoAdd(Settings.Default.DefaultPartition, key, entry, utcExpiry, null);
        }

        public void Clear(bool ignoreExpirationDate = false)
        {
            using (var ctx = CacheContext.Create(_connectionString))
            {
                var clearQuery = SQL
                    .DELETE_FROM("[CACHE_ITEM]")
                    ._If(!ignoreExpirationDate, "[EXPIRY] IS NOT NULL AND [EXPIRY] <= {0}", DateTime.UtcNow);
                
                ctx.Execute(clearQuery);
            }
        }

        public object Get(string partition, string key)
        {
            using (var ctx = CacheContext.Create(_connectionString))
            {
                var query = SQL
                   .SELECT("[VALUE]")
                   .FROM("[CACHE_ITEM]")
                   .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key)
                   ._("([EXPIRY] IS NULL OR [EXPIRY] > {0})", DateTime.UtcNow);
                
                var item = ctx.Map<CacheItem>(query).FirstOrDefault();
                return (item == null || item.Value == null) ? null : Deserialize(item.Value);
            }
        }

        public override object Get(string key)
        {
            return Get(Settings.Default.DefaultPartition, key);
        }

        public override void Remove(string key)
        {
            string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            if (File.Exists(path))

                File.Delete(path);
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            //var item = new CacheItem {Expires = utcExpiry, Item = entry};

            //string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            //using (FileStream file = File.OpenWrite(path))
            //{
            //    var formatter = new BinaryFormatter();

            //    formatter.Serialize(file, item);
            //}
        }

        #region Serialization

        private static object Deserialize(byte[] array)
        {
            using (var memoryStream = new MemoryStream(array))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(memoryStream);
            }
        }

        private static byte[] Serialize(object obj)
        {
            var formatter = new BinaryFormatter();
            using (var stream = new MemoryStream())
            {
                formatter.Serialize(stream, obj);
                return stream.ToArray();
            }
        }

        #endregion

        #region Private Methods

        private object DoAdd(string partition, string key, object value, DateTime? utcExpiry, TimeSpan? interval)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);

            var formattedValue = Serialize(value);

            using (var ctx = CacheContext.Create(_connectionString))
            using (ctx.Transaction = ctx.Connection.BeginTransaction())
            {
                try
                {
                    var query = SQL
                       .SELECT("*")
                       .FROM("[CACHE_ITEM]")
                       .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key);
                    
                    var item = ctx.Map<CacheItem>(query).FirstOrDefault();
                    if (item == null)
                    {
                        // Key not in the cache
                        var insert = SQL
                            .INSERT_INTO("[CACHE_ITEM]")
                            .VALUES(partition, key, formattedValue, utcExpiry, interval);
                        ctx.Execute(insert);
                    }
                    else
                    {
                       var update = SQL
                          .UPDATE("[CACHE_ITEM]")
                          .SET("[VALUE] = {0}, [EXPIRY] = {1}", SQL.Param(formattedValue), utcExpiry)
                          .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key);
                       ctx.Execute(update);
                    }
                    ctx.Transaction.Commit();
                }
                catch
                {
                    ctx.Transaction.Rollback();
                    throw;
                }
            }

            return value;
        }

        private static string CreateConnectionString(string dbPath)
        {
            var fmt = Settings.Default.ConnectionStringFormat;
            return String.Format(fmt, dbPath);
        }

        #endregion
    }
}