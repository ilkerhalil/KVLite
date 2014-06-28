using System;
using System.Configuration;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web;
using System.Web.Caching;
using System.Web.Security;

namespace KVLite
{
    public sealed class FileCache : OutputCacheProvider
    {
        private string _cachePath;

        private string CachePath
        {
            get
            {
                if (!string.IsNullOrEmpty(_cachePath))

                    return _cachePath;

                _cachePath = ConfigurationManager.AppSettings["OutputCachePath"];

                if (_cachePath == null) _cachePath = "~/";

                HttpContext context = HttpContext.Current;

                if (context != null)
                {
                    _cachePath = context.Server.MapPath(_cachePath);

                    if (!_cachePath.EndsWith("\\"))

                        _cachePath += "\\";
                }

                return _cachePath;
            }
        }

        public object this[string cacheKey]
        {
            get { return Get(cacheKey); }

            set { Add(cacheKey, value, DateTime.Now.AddDays(365)); }
        }

        public override object Add(string key, object entry, DateTime utcExpiry)
        {
            string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            if (File.Exists(path))

                return entry;

            using (FileStream file = File.OpenWrite(path))
            {
                var item = new CacheItem {Expires = utcExpiry, Item = entry};

                var formatter = new BinaryFormatter();

                formatter.Serialize(file, item);
            }

            return entry;
        }

        public override object Get(string key)
        {
            string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            if (!File.Exists(path))

                return null;

            CacheItem item = null;

            using (FileStream file = File.OpenRead(path))
            {
                var formatter = new BinaryFormatter();

                item = (CacheItem) formatter.Deserialize(file);
            }

            if (item == null || item.Expires <= DateTime.Now.ToUniversalTime())
            {
                Remove(key);

                return null;
            }

            return item.Item;
        }

        public override void Remove(string key)
        {
            string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            if (File.Exists(path))

                File.Delete(path);
        }

        public override void Set(string key, object entry, DateTime utcExpiry)
        {
            var item = new CacheItem {Expires = utcExpiry, Item = entry};

            string path = CachePath + FormsAuthentication.HashPasswordForStoringInConfigFile(key, "MD5") + ".dat";

            using (FileStream file = File.OpenWrite(path))
            {
                var formatter = new BinaryFormatter();

                formatter.Serialize(file, item);
            }
        }
    }
}