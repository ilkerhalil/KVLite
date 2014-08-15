namespace KVLite
{
    public interface ICache
    {
        object AddPersistent(string partition, string key, object value);

        object AddPersistent(string key, object value);

        void Clear();

        void Clear(CacheReadMode cacheReadMode);

        int Count();

        int Count(CacheReadMode cacheReadMode);

        long LongCount();

        long LongCount(CacheReadMode cacheReadMode);
    }

    public interface ICache<TCache> : ICache where TCache : class, ICache<TCache>, new()
    {
    }

    public enum CacheReadMode : byte
    {
        IgnoreExpirationDate = 0,
        ConsiderExpirationDate = 1
    }
}
