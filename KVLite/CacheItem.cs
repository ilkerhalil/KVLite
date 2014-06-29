using System;
using KVLite.Dapper;

namespace KVLite
{
    /// <summary>
    /// 
    /// </summary>
    [Table("CACHE_ITEM")]
    internal sealed class CacheItem
    {
        /// <summary>
        /// 
        /// </summary>
        [Key]
        public long Id { get; set; }

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