using PommaLabs.KVLite.Core;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Settings used by <see cref="MemoryCache"/>.
    /// </summary>
    [Serializable]
    public sealed class MemoryCacheSettings : AbstractCacheSettings
    {
        #region Fields

        string _cacheName = MemoryCacheConfiguration.Instance.DefaultCacheName;

        #endregion Fields

        #region Construction

        /// <summary>
        ///   Sets default values read from <see cref="MemoryCacheConfiguration"/>.
        /// </summary>
        public MemoryCacheSettings()
        {
            DefaultPartition = MemoryCacheConfiguration.Instance.DefaultPartition;
            StaticIntervalInDays = MemoryCacheConfiguration.Instance.DefaultStaticIntervalInDays;
        }

        #endregion Construction

        #region Properties

        /// <summary>
        ///   Gets the default settings for <see cref="MemoryCache"/>.
        /// </summary>
        /// <value>The default settings for <see cref="MemoryCache"/>.</value>
        [Pure]
        public static MemoryCacheSettings Default { get; } = new MemoryCacheSettings();

        #endregion Properties

        #region Settings

        /// <summary>
        ///   The name of the in-memory store used as the backend for the cache.
        /// </summary>
        public string CacheName
        {
            get
            {
                var result = _cacheName;

                // Postconditions
                Debug.Assert(!string.IsNullOrWhiteSpace(result));
                return result;
            }
            set
            {
                // Preconditions
                Raise<ArgumentException>.If(string.IsNullOrWhiteSpace(value), ErrorMessages.NullOrEmptyCacheName);
                Raise<ArgumentException>.IfNot(Regex.IsMatch(value, @"^[a-zA-Z0-9_\-\. ]*$"), ErrorMessages.InvalidCacheName);

                _cacheName = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        ///   Gets the cache URI; used for logging.
        /// </summary>
        /// <value>The cache URI.</value>
        public override string CacheUri => CacheName;

        #endregion Settings
    }
}
