using System;
using System.Diagnostics.Contracts;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Properties;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    public sealed class PersistentCacheSettings : CacheSettingsBase
    {
        private string _cacheFile = Settings.Default.DefaultPersistentCacheFile;
        private int _maxCacheSizeInMB = Settings.Default.DefaultMaxPersistentCacheSizeInMB;

        public string CacheFile
        {
            get
            {
                Contract.Ensures(!String.IsNullOrWhiteSpace(Contract.Result<string>()));
                return _cacheFile;
            }
            set
            {
                Contract.Requires<ArgumentException>(!String.IsNullOrWhiteSpace(value), ErrorMessages.NullOrEmptyCachePath);
                _cacheFile = value;
                OnPropertyChanged("CacheFile");
            }
        }

        public int MaxCacheSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _maxCacheSizeInMB;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _maxCacheSizeInMB = value;
                OnPropertyChanged("MaxCacheSizeInMB");
            }
        }
    }
}
