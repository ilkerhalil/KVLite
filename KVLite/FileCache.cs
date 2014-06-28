using System;
using System.Configuration;
using System.IO;
using System.Transactions;
using System.Web.Caching;
using System.Web.Security;
using KVLite.Properties;
using Thrower;

namespace KVLite
{
    public sealed class FileCache : OutputCacheProvider
    {
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
            _connectionString = CreateConnectionString(cachePath, Settings.Default.DefaultMaxCacheSize);
            
            using (var cache = new CacheContext(_connectionString))
            {
                cache.Database.CreateIfNotExists();    
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

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            //string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            //if (File.Exists(path))

            //    return entry;

            //using (FileStream file = File.OpenWrite(path))
            //{
            //    var item = new CacheItem {Expires = utcExpiry, Item = entry};

            //    var formatter = new BinaryFormatter();

            //    formatter.Serialize(file, item);
            //}

            //return entry;
            return null;
        }

        public void Clear()
        {
            using (var ctx = new CacheContext(_connectionString))
            {
                using (var tr = new TransactionScope())
                {
                    ctx.CacheItems.RemoveRange(ctx.CacheItems);
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
            return null;
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

        private static string CreateConnectionString(string dbPath, int maxDbSize)
        {
                var fmt = Settings.Default.ConnectionStringFormat;
                return String.Format(fmt, dbPath, maxDbSize);
            
        }
    }
}