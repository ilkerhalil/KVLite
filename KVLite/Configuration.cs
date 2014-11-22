//
// Configuration.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Configuration;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class Configuration : ConfigurationSection
    {
        private const string SectionName = "pommaLabs.kvlite";
        private const string DefaultStaticIntervalInDaysKey = "DefaultStaticIntervalInDays";
        private const string MaxCacheSizeInMBKey = "MaxCacheSizeInMB";
        private const string MaxLogSizeInMBKey = "MaxLogSizeInMB";
        private const string MaxCachedConnectionCountKey = "MaxCachedConnectionCount";
        private const string MaxCachedSerializerCountKey = "MaxCachedSerializerCount";
        private const string NancyCacheKindKey = "NancyCacheKind";
        private const string OperationCountBeforeSoftCleanupKey = "OperationCountBeforeSoftCleanup";

        private static readonly Configuration CachedInstance = (Configuration) ConfigurationManager.GetSection(SectionName);

        public static Configuration Instance
        {
            get { return CachedInstance; }
        }

        [ConfigurationProperty(DefaultStaticIntervalInDaysKey, IsRequired = false, DefaultValue = 30)]
        public int DefaultStaticIntervalInDays
        {
            get { return Convert.ToInt32(this[DefaultStaticIntervalInDaysKey]); }
        }

        [ConfigurationProperty(MaxCachedConnectionCountKey, IsRequired = false, DefaultValue = 10)]
        public int MaxCachedConnectionCount
        {
            get { return Convert.ToInt16(this[MaxCachedConnectionCountKey]); }
        }

        [ConfigurationProperty(MaxCachedSerializerCountKey, IsRequired = false, DefaultValue = 10)]
        public int MaxCachedSerializerCount
        {
            get { return Convert.ToInt16(this[MaxCachedSerializerCountKey]); }
        }

        [ConfigurationProperty(MaxCacheSizeInMBKey, IsRequired = true)]
        public int MaxCacheSizeInMB
        {
            get { return Convert.ToInt32(this[MaxCacheSizeInMBKey]); }
        }

        [ConfigurationProperty(MaxLogSizeInMBKey, IsRequired = true)]
        public int MaxLogSizeInMB
        {
            get { return Convert.ToInt32(this[MaxLogSizeInMBKey]); }
        }

        [ConfigurationProperty(NancyCacheKindKey, IsRequired = false)]
        public CacheKind NancyCacheKind
        {
            get { return (CacheKind) this[NancyCacheKindKey]; }
        }

        [ConfigurationProperty(OperationCountBeforeSoftCleanupKey, IsRequired = false, DefaultValue = 100)]
        public int OperationCountBeforeSoftCleanup
        {
            get { return Convert.ToInt16(this[OperationCountBeforeSoftCleanupKey]); }
        }
    }
}