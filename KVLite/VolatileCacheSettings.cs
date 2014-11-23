using System;
using System.Diagnostics.Contracts;
using System.Runtime.Caching;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    public sealed class VolatileCacheSettings : CacheSettingsBase
    {
        private MemoryCache _memoryCache = MemoryCache.Default;

        public MemoryCache MemoryCache
        {
            get
            {
                Contract.Ensures(Contract.Result<MemoryCache>() != null);
                return _memoryCache;
            }
            set
            {
                Contract.Requires<ArgumentNullException>(value != null);
                _memoryCache = value;
                OnPropertyChanged("MemoryCache");
            }
        }
    }
}
