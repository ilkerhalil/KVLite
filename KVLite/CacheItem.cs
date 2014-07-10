using System;
using System.Data;
using System.Data.Linq;
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
        [Column(IsPrimaryKey = true)]
        public string Partition { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(IsPrimaryKey = true)]
        public string Key { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(CanBeNull = false, DbType = "Image")]
        public byte[] Value { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(CanBeNull = true)]
        public DateTime? Expiry { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Column(CanBeNull = true)]
        public TimeSpan? Interval { get; set; }
    }
}