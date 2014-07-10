using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Caching;
using System.Web.Security;
using DbExtensions;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    public sealed class FileCache : OutputCacheProvider
    {
        private static readonly FileCache DefaultInstance = new FileCache();

        private readonly string _cachePath;
        private readonly string _connectionString;

        public FileCache() : this(ConfigurationManager.AppSettings["OutputCachePath"] ?? Settings.Default.DefaultCachePath)
        {

            //if (_cachePath == null) _cachePath = "~/";

            //HttpContext context = HttpContext.Current;

            //if (context != null)
            //{
            //    _cachePath = context.Server.MapPath(_cachePath);

            //    if (!_cachePath.EndsWith("\\"))

            //        _cachePath += "\\";
            //}
        }

        public FileCache(string cachePath)
        {
            Raise<ArgumentException>.IfIsEmpty(cachePath, ErrorMessages.NullOrEmptyCachePath);

            _cachePath = cachePath;
            _connectionString = CreateConnectionString(cachePath);

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

        public static FileCache Default
        {
            get { return DefaultInstance; }
        }

        public object this[string cacheKey]
        {
            get { return Get(cacheKey); }

            set { Add(cacheKey, value, DateTime.Now.AddDays(365)); }
        }

        #endregion

        public override object Add(string key, object entry, DateTime utcExpiry)
        {

            //using (FileStream file = File.OpenWrite(path))
            //{
            //    var item = new CacheItem {Expires = utcExpiry, Item = entry};

            //    var formatter = new BinaryFormatter();

            //    formatter.Serialize(file, item);
            //}

            //return entry;

            var formatter = new BinaryFormatter();
            var stream = new MemoryStream();
            formatter.Serialize(stream, entry);
            var formattedValue = stream.ToArray();

            


            using (var ctx = CacheContext.Create(_connectionString))
            using (ctx.Transaction = ctx.Connection.BeginTransaction())
            {
                try
                {
                   var query = SQL
                      .SELECT("*")
                      .FROM("[CACHE_ITEM]")
                      .WHERE("[KEY] = {0}", key);
                    var item = ctx.Map<CacheItem>(query).FirstOrDefault();
                    if (item == null)
                    {
                        // Key not in the cache
                        ctx.Table<CacheItem>().Add(new CacheItem {Partition = "A", Key = key, Expiry = utcExpiry, Value = formattedValue});
                    }
                    else
                    {
                        item.Value = formattedValue;
                        item.Expiry = utcExpiry;
                        ctx.Table<CacheItem>().Update(item);
                    }
                    ctx.Transaction.Commit();
                }
                catch
                {
                    ctx.Transaction.Rollback();
                    throw;
                }
            }
            
            return entry;
        }

        public void Clear(bool ignoreExpirationDate = false)
        {
            using (var ctx = CacheContext.Create(_connectionString))
            {
                if (ignoreExpirationDate)
                {
                    ctx.Execute("DELETE FROM cache_item");
                }
                else
                {
                    ctx.Execute("DELETE FROM cache_item WHERE expiry <= @UtcNow", DateTime.UtcNow);
                }
            }
        }

        public override object Get(string key)
        {
            //string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            //if (!File.Exists(path))

            //    return null;

            //CacheItem item = null;

            //using (FileStream file = File.OpenRead(path))
            //{
            //    var formatter = new BinaryFormatter();

            //    item = (CacheItem) formatter.Deserialize(file);
            //}

            //if (item == null || item.Expires <= DateTime.Now.ToUniversalTime())
            //{
            //    Remove(key);

            //    return null;
            //}

            //return item.Item;

            using (var ctx = CacheContext.Create(_connectionString))
            {
               var query = SQL
                  .SELECT("[VALUE]")
                  .FROM("[CACHE_ITEM]")
                  .WHERE("[KEY] = {0} AND [EXPIRY] > {1}", key, DateTime.UtcNow);
                var item = ctx.Map<CacheItem>(query).FirstOrDefault();
                return (item == null || item.Value == null) ? null : Deserialize(item.Value.ToArray());
            }
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

        public void AddSliding<TObj>(string partition, string key, TObj value, TimeSpan interval)
        {
            
        }

        public void AddPersistent<TObj>(string partition, string key, TObj value)
        {
            Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition);
            Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey);
        }

        public void AddPersistent<TObj>(string key, TObj value)
        {
            AddPersistent(Settings.Default.DefaultPartition, key, value);
        }

        #region Private Methods

        private static object Deserialize(byte[] array)
        {
            using (var memoryStream = new MemoryStream(array))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(memoryStream);
            }
        }

        private static string CreateConnectionString(string dbPath)
        {
            var fmt = Settings.Default.ConnectionStringFormat;
            return String.Format(fmt, dbPath);
        }

        #endregion
    }
}