using PommaLabs.KVLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Caching;
using System.Text;

namespace Examples
{
    class Program
    {
        static void Main(string[] args)
        {
            // Volatile cache will use this memory cache as its backend.
            var myMemoryCache = new MemoryCache("My Memory Cache");

            // Settings that we will use in new volatile caches.
            var volatileCacheSettings = new VolatileCacheSettings
            {
                MemoryCache = myMemoryCache, // The backend
                StaticIntervalInDays = 10 // How many days static values will last
            };
        }
    }
}
