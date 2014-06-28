using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        [Key, Column("CAIT_KEY")]
        public string Key { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required, Column("CAIT_EXPIRES_ON")]
        public DateTime ExpiresOn { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required, Column("CAIT_VALUE")]
        public string Value { get; set; }
    }
}