﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.0
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace PommaLabs.KVLite.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "12.0.0.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        /// <summary>
        /// Default SQLite DB for the persistent cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default SQLite DB for the persistent cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PersistentCache.sqlite")]
        public string PersistentCache_DefaultCacheFile {
            get {
                return ((string)(this["PersistentCache_DefaultCacheFile"]));
            }
        }
        
        /// <summary>
        /// Default interval for &quot;static&quot; items.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default interval for \"static\" items.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int AllCaches_DefaultStaticIntervalInDays {
            get {
                return ((int)(this["AllCaches_DefaultStaticIntervalInDays"]));
            }
        }
        
        /// <summary>
        /// Default max cache size for persistent cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default max cache size for persistent cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1024")]
        public int PersistentCache_DefaultMaxCacheSizeInMB {
            get {
                return ((int)(this["PersistentCache_DefaultMaxCacheSizeInMB"]));
            }
        }
        
        /// <summary>
        /// Default number of inserts before a cleanup is issued.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default number of inserts before a cleanup is issued.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("64")]
        public int PersistentCache_DefaultInsertionCountBeforeAutoClean {
            get {
                return ((int)(this["PersistentCache_DefaultInsertionCountBeforeAutoClean"]));
            }
        }
        
        /// <summary>
        /// The ICache implementation that will be used by the NancyFX caching bootstrapper.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The ICache implementation that will be used by the NancyFX caching bootstrapper.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PommaLabs.KVLite.PersistentCache, PommaLabs.KVLite")]
        public string Nancy_ResponseCacheType {
            get {
                return ((string)(this["Nancy_ResponseCacheType"]));
            }
        }
        
        /// <summary>
        /// Default max journal size for persistent cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default max journal size for persistent cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        public int PersistentCache_DefaultMaxJournalSizeInMB {
            get {
                return ((int)(this["PersistentCache_DefaultMaxJournalSizeInMB"]));
            }
        }
        
        /// <summary>
        /// Default partition, used when none is specified.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default partition, used when none is specified.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.DefaultPartition")]
        public string AllCaches_DefaultPartition {
            get {
                return ((string)(this["AllCaches_DefaultPartition"]));
            }
        }
        
        /// <summary>
        /// The partition used by Nancy response cache items.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The partition used by Nancy response cache items.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Nancy.ResponseCache")]
        public string Nancy_ResponseCachePartition {
            get {
                return ((string)(this["Nancy_ResponseCachePartition"]));
            }
        }
        
        /// <summary>
        /// The ICache implementation that will be used by the Web Forms viewstate persister.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The ICache implementation that will be used by the Web Forms viewstate persister." +
            "")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PommaLabs.KVLite.PersistentCache, PommaLabs.KVLite")]
        public string Web_ViewStatePersisterType {
            get {
                return ((string)(this["Web_ViewStatePersisterType"]));
            }
        }
        
        /// <summary>
        /// The partition used by ViewState items.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The partition used by ViewState items.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Web.ViewStates")]
        public string Web_ViewStatePersisterPartition {
            get {
                return ((string)(this["Web_ViewStatePersisterPartition"]));
            }
        }
        
        /// <summary>
        /// The ICache implementation that will be used by the Web Forms cache provider.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The ICache implementation that will be used by the Web Forms cache provider.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PommaLabs.KVLite.PersistentCache, PommaLabs.KVLite")]
        public string Web_OutputCacheProviderType {
            get {
                return ((string)(this["Web_OutputCacheProviderType"]));
            }
        }
        
        /// <summary>
        /// The partition used by output cache provider items.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The partition used by output cache provider items.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Web.OutputCache")]
        public string Web_OutputCacheProviderPartition {
            get {
                return ((string)(this["Web_OutputCacheProviderPartition"]));
            }
        }
        
        /// <summary>
        /// The default cache name used by the in-memory cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The default cache name used by the in-memory cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("VolatileCache")]
        public string VolatileCache_DefaultCacheName {
            get {
                return ((string)(this["VolatileCache_DefaultCacheName"]));
            }
        }
        
        /// <summary>
        /// Default number of inserts before a cleanup is issued.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default number of inserts before a cleanup is issued.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("64")]
        public int VolatileCache_DefaultInsertionCountBeforeAutoClean {
            get {
                return ((int)(this["VolatileCache_DefaultInsertionCountBeforeAutoClean"]));
            }
        }
        
        /// <summary>
        /// Default max cache size for volatile cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default max cache size for volatile cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("256")]
        public int VolatileCache_DefaultMaxCacheSizeInMB {
            get {
                return ((int)(this["VolatileCache_DefaultMaxCacheSizeInMB"]));
            }
        }
        
        /// <summary>
        /// Default max journal size for volatile cache.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("Default max journal size for volatile cache.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        public int VolatileCache_DefaultMaxJournalSizeInMB {
            get {
                return ((int)(this["VolatileCache_DefaultMaxJournalSizeInMB"]));
            }
        }
        
        /// <summary>
        /// The ICache implementation that will be used by the Web API output cache provider.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The ICache implementation that will be used by the Web API output cache provider." +
            "")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PommaLabs.KVLite.PersistentCache, PommaLabs.KVLite")]
        public string Web_Http_ApiOutputCacheProviderType {
            get {
                return ((string)(this["Web_Http_ApiOutputCacheProviderType"]));
            }
        }
        
        /// <summary>
        /// The partition used by Web API output cache provider items.
        /// </summary>
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Configuration.SettingsDescriptionAttribute("The partition used by Web API output cache provider items.")]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Web.Http.ApiOutputCache")]
        public string Web_Http_ApiOutputCacheProviderPartition {
            get {
                return ((string)(this["Web_Http_ApiOutputCacheProviderPartition"]));
            }
        }
    }
}
