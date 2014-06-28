using System;
using System.Configuration;
using System.IO;
using System.Linq;
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
        private CacheContext _tmpContext;

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

            var ctx = GetContext();
            ctx.Database.CreateIfNotExists();
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

            var ctx = GetContext();
            var item = ctx.CacheItems.Add(new CacheItem {Key = key, ExpiresOn = utcExpiry, Value = entry.ToString()});
            ctx.SaveChanges();
            return item;
        }

        public void Clear(bool ignoreExpirationDate = false)
        {
            var ctx = GetContext();
            using (var transaction = ctx.Database.BeginTransaction())
            {
                try
                {
                    var utcNow = DateTime.UtcNow;
                    var items = ignoreExpirationDate
                        ? ctx.CacheItems
                        : ctx.CacheItems.Where(ci => ci.ExpiresOn <= utcNow);
                    ctx.CacheItems.RemoveRange(items);
                    ctx.SaveChanges();
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
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

            var ctx = GetContext();
            var utcNow = DateTime.UtcNow;
            var item = ctx.CacheItems.FirstOrDefault(x => x.Key == key && x.ExpiresOn > utcNow);
            return (item == null) ? null : item.Value;
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

        #region Private Methods

        private static string CreateConnectionString(string dbPath, int maxDbSize)
        {
            var fmt = Settings.Default.ConnectionStringFormat;
            return String.Format(fmt, dbPath, maxDbSize);
        }

        private CacheContext GetContext()
        {
            if (_tmpContext == null)
            {
                _tmpContext = new CacheContext(_connectionString);
            }
            return _tmpContext;
        }

        #endregion
    }
}