using Finsa.CodeServices.Common.Portability;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Westwind.Utilities.Configuration;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Configuration class for the <see cref="MemoryCache"/>. Default values are set inside
    ///   the configuration file itself.
    /// </summary>
    [Serializable, CLSCompliant(false)]
    public sealed class MemoryCacheConfiguration : AppConfiguration
    {
        #region Static instance

        static MemoryCacheConfiguration()
        {
            var configurationFile = "KVLite.config";
            if (PortableEnvironment.AppIsRunningOnAspNet)
            {
                // If application is running on ASP.NET, then we look for the configuration file
                // inside the root of the project. Usually, configuration file are not stored into
                // the "bin" directory, because every change would make the application restart.
                configurationFile = "~/" + configurationFile;
            }
            Instance = new MemoryCacheConfiguration();
            Instance.Initialize(new ConfigurationFileConfigurationProvider<MemoryCacheConfiguration>
            {
                ConfigurationFile = PortableEnvironment.MapPath(configurationFile),
                ConfigurationSection = "memoryCache"
            });
        }

        /// <summary>
        ///   Gets the static configuration instance.
        /// </summary>
        /// <value>The static configuration instance.</value>
        public static MemoryCacheConfiguration Instance { get; }

        #endregion Static instance

        /// <summary>
        ///   Initializes a new instance of the <see cref="MemoryCacheConfiguration"/> class and
        ///   sets the default values for each configuration entry.
        /// </summary>
        public MemoryCacheConfiguration()
        {
            DefaultCacheName = nameof(MemoryCache);
            DefaultPartition = "KVLite.DefaultPartition";
            DefaultStaticIntervalInDays = 30;
        }

        /// <summary>
        ///   Gets or sets the default name of the cache, that is, the default cache name used by
        ///   the in-memory cache.
        /// </summary>
        /// <value>The default name of the cache.</value>
        public string DefaultCacheName { get; set; }

        /// <summary>
        ///   Gets or sets the default partition, used when none is specified.
        /// </summary>
        /// <value>The default partition, used when none is specified.</value>
        public string DefaultPartition { get; set; }

        /// <summary>
        ///   Gets or sets the default static interval in days, that is, the default interval for
        ///   "static" items.
        /// </summary>
        /// <value>The default static interval in days.</value>
        public int DefaultStaticIntervalInDays { get; set; }
    }
}
