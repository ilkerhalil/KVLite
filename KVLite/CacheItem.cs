using System;

namespace KVLite
{
    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    internal sealed class CacheItem
    {
        public DateTime Expires;

        public object Item;
    }
}