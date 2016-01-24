// File name: MemoryCacheConfiguration.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using Finsa.CodeServices.Common.Portability;
using System;
using Westwind.Utilities.Configuration;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Configuration class for the <see cref="MemoryCache"/>. Default values are set inside the
    ///   configuration file itself.
    /// </summary>
    [Serializable, CLSCompliant(false)]
    public sealed class MemoryCacheConfiguration : AppConfiguration
    {
        #region Static instance

        /// <summary>
        ///   Gets the static configuration instance.
        /// </summary>
        /// <value>The static configuration instance.</value>
        public static MemoryCacheConfiguration Instance { get; } = InitializeInstance();

        static MemoryCacheConfiguration InitializeInstance()
        {
            var configurationFile = "KVLite.config";
            if (PortableEnvironment.AppIsRunningOnAspNet)
            {
                // If application is running on ASP.NET, then we look for the configuration file
                // inside the root of the project. Usually, configuration file are not stored into
                // the "bin" directory, because every change would make the application restart.
                configurationFile = "~/" + configurationFile;
            }

            var instance = new MemoryCacheConfiguration();
            instance.Initialize(new ConfigurationFileConfigurationProvider<MemoryCacheConfiguration>
            {
                ConfigurationFile = PortableEnvironment.MapPath(configurationFile),
                ConfigurationSection = "memoryCache"
            });

            return instance;
        }

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
            DefaultMaxCacheSizeInMB = 256;
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

        /// <summary>
        ///   Gets or sets the default maximum cache size in MB.
        /// </summary>
        /// <value>The default maximum cache size in MB.</value>
        public int DefaultMaxCacheSizeInMB { get; set; }
    }
}
