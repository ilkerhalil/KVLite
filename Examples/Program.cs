using System.Runtime.Caching;
using PommaLabs.KVLite;

namespace Examples
{
    internal static class Program
    {
        internal static void Main(string[] args)
        {
            // Volatile cache will use this memory cache as its backend.
            var myMemoryCache = new MemoryCache("My Memory Cache");

            // Settings that we will use in new volatile caches.
            var volatileCacheSettings = new VolatileCacheSettings {
                MemoryCache = myMemoryCache, // The backend.
                StaticIntervalInDays = 10 // How many days static values will last.
            };

            // Settings that we will use in new persistent caches.
            var persistentCacheSettings = new PersistentCacheSettings {
                CacheFile = "PersistentCache.sqlite", // The SQLite DB used as the backend for the cache.
                InsertionCountBeforeCleanup = 10, // Number of inserts before a cache cleanup is issued.
                MaxCacheSizeInMB = 64, // Max size in megabytes for the cache.
                MaxJournalSizeInMB = 16, // Max size in megabytes for the SQLite journal log.
                StaticIntervalInDays = 10 // How many days static values will last.
            };
        }
    }
}