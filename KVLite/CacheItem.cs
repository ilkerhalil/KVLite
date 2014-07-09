using System;
using System.Data.Linq.Mapping;

namespace KVLite
{
    /// <summary>
    /// 
    /// </summary>
    [Table(Name = "CACHE_ITEM")]
    internal sealed class CacheItem
    {
        /// <summary>
        /// 
        /// </summary>
        public string Partition { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime Expiry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public byte[] Value { get; set; }
    }
}