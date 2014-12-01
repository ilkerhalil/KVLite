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
        private int _maxJournalSizeInMB = Settings.Default.PCache_DefaultMaxLogSizeInMB;
        private int _insertionCountBeforeCleanup = Settings.Default.PCache_DefaultInsertionCountBeforeCleanup;

        /// <summary>
        ///   The SQLite DB used as the backend for the cache.
        /// </summary>
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

        /// <summary>
        ///   Number of inserts before a cache cleanup is issued.
        /// </summary>
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

        /// <summary>
        ///   Max size in megabytes for the cache.
        /// </summary>
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

        /// <summary>
        ///   Max size in megabytes for the SQLite journal log.
        /// </summary>
        public int MaxJournalSizeInMB
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() > 0);
                return _maxJournalSizeInMB;
            }
            set
            {
                Contract.Requires<ArgumentOutOfRangeException>(value > 0);
                _maxJournalSizeInMB = value;
                OnPropertyChanged("MaxJournalSizeInMB");
            }
        }
    }
}
