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
        private string _cacheFile = Settings.Default.PCache_DefaultFile;
        private int _maxCacheSizeInMB = Settings.Default.PCache_DefaultMaxCacheSizeInMB;
        private int _maxLogSizeInMB = Settings.Default.PCache_DefaultMaxLogSizeInMB;
        private int _insertionCountBeforeCleanup = Settings.Default.PCache_DefaultInsertionCountBeforeCleanup;

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

        public int InsertionCountBeforeCleanup
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _insertionCountBeforeCleanup;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _insertionCountBeforeCleanup = value;
                OnPropertyChanged("InsertionCountBeforeCleanup");
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

        public int MaxLogSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _maxLogSizeInMB;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _maxLogSizeInMB = value;
                OnPropertyChanged("MaxLogSizeInMB");
            }
        }
    }
}
