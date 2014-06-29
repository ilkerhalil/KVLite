using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Caching;
using System.Web.Security;
using Dapper;
using KVLite.Dapper;
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

            using (var ctx = CacheContext.Create(_connectionString))
            using (var transaction = ctx.BeginTransaction())
            {
                try
                {
                    var cacheReady = ctx.Query<long>("SELECT COUNT(*) FROM sqlite_master WHERE name = 'cache_item'").First() == 1L;
                    if (!cacheReady)
                    {
                        ctx.Execute(Settings.Default.CacheCreationScript);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
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
            using (var transaction = ctx.BeginTransaction())
            {
                try
                {
                    var item = ctx.Get<CacheItem>(key);
                    if (item == null)
                    {
                        // Key not in the cache
                        ctx.Insert(new CacheItem {Key = key, Expiry = utcExpiry, Value = formattedValue});
                    }
                    else
                    {
                        item.Value = formattedValue;
                        item.Expiry = utcExpiry;
                        ctx.Update(item);
                    }
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
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
                const string query = "SELECT VALUE FROM cache_item WHERE key = @key AND expiry > @UtcNow";
                var item = ctx.Query<CacheItem>(query, new {key, DateTime.UtcNow}).FirstOrDefault();
                return (item == null || item.Value == null) ? null : Deserialize(item.Value);
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

        #region Private Methods

        private static object Deserialize(byte[] array)
        {
            using (var memoryStream = new MemoryStream(array))
            {
                var formatter = new BinaryFormatter();
                return formatter.Deserialize(memoryStream);
            }
        }

        private static string CreateConnectionString(string dbPath, int maxDbSize)
        {
            var fmt = Settings.Default.ConnectionStringFormat;
            return String.Format(fmt, dbPath, maxDbSize);
        }

        #endregion
    }
}