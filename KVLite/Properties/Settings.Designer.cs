﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.34209
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
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("PersistentCache.sqlite")]
        public string PersistentCache_DefaultCacheFile {
            get {
                return ((string)(this["PersistentCache_DefaultCacheFile"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("30")]
        public int AllCaches_DefaultStaticIntervalInDays {
            get {
                return ((int)(this["AllCaches_DefaultStaticIntervalInDays"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("1024")]
        public int PersistentCache_DefaultMaxCacheSizeInMB {
            get {
                return ((int)(this["PersistentCache_DefaultMaxCacheSizeInMB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("64")]
        public int PersistentCache_DefaultInsertionCountBeforeAutoClean {
            get {
                return ((int)(this["PersistentCache_DefaultInsertionCountBeforeAutoClean"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Persistent")]
        public string Nancy_ResponseCacheKind {
            get {
                return ((string)(this["Nancy_ResponseCacheKind"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("32")]
        public int PersistentCache_DefaultMaxLogSizeInMB {
            get {
                return ((int)(this["PersistentCache_DefaultMaxLogSizeInMB"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.DefaultPartition")]
        public string AllCaches_DefaultPartition {
            get {
                return ((string)(this["AllCaches_DefaultPartition"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Nancy.ResponseCache")]
        public string Nancy_ResponseCachePartition {
            get {
                return ((string)(this["Nancy_ResponseCachePartition"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Persistent")]
        public string Web_ViewStatePersisterKind {
            get {
                return ((string)(this["Web_ViewStatePersisterKind"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Web.ViewStates")]
        public string Web_ViewStatePersisterPartition {
            get {
                return ((string)(this["Web_ViewStatePersisterPartition"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("Persistent")]
        public string Web_OutputCacheProviderKind {
            get {
                return ((string)(this["Web_OutputCacheProviderKind"]));
            }
        }
        
        [global::System.Configuration.ApplicationScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("KVLite.Web.OutputCache")]
        public string Web_OutputCacheProviderPartition {
            get {
                return ((string)(this["Web_OutputCacheProviderPartition"]));
            }
        }
    }
}