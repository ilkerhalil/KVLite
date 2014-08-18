namespace PommaLabs.KVLite.Core
{
    internal static class Queries
    {
        public const string CacheSchema = @"
            PRAGMA auto_vacuum = FULL;
            CREATE TABLE CacheItem (
                partition TEXT NOT NULL,
                key TEXT NOT NULL,
                serializedValue BLOB NOT NULL,
                utcCreation BIGINT NOT NULL,
                utcExpiry BIGINT,
                interval BIGINT,
                CONSTRAINT CacheItem_PK PRIMARY KEY (partition, key)
            );
            CREATE INDEX UtcExpiry_Idx ON CacheItem (utcExpiry ASC);
        ";

        public const string Clear = @"
            delete from CacheItem
             where @ignoreExpirationDate = 1
                or (utcExpiry is not null and utcExpiry <= strftime('%s', 'now')); -- Clear only invalid rows
        ";

        public const string Contains = @"
            select interval is not null as sliding
              from CacheItem
             where partition = @partition
               and key = @key
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Select only valid rows
        ";

        public const string Count = @"
            select count(*)
              from CacheItem
             where @ignoreExpirationDate = 1
                or (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Select only valid rows
        ";

        public const string DoAdd = @"
            insert or replace into CacheItem (partition, key, serializedValue, utcCreation, utcExpiry, interval)
            values (@partition, @key, @serializedValue, strftime('%s', 'now'), @utcExpiry, @interval);
        ";

        public const string DoGetAllItems = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where interval is not null
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Update only valid rows
            select *
              from CacheItem
             where (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Select only valid rows
        ";

        public const string DoGetPartitionItems = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where partition = @partition
               and interval is not null
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Update only valid rows
            select * 
              from CacheItem
             where partition = @partition
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Select only valid rows
        ";

        public const string GetItem = @"
            select *
              from CacheItem
             where partition = @partition
               and key = @key
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now'))-- Select only valid rows
        ";

        public const string Remove = @"
            delete from CacheItem
             where partition = @partition
               and key = @key;
        ";

        public const string SchemaIsReady = @"
            select count(*)
              from sqlite_master
             where name = 'CacheItem';
        ";

        public const string SetPragmas = @"
            PRAGMA read_uncommitted = 1;
            PRAGMA cache_spill = 1;
            PRAGMA temp_store = MEMORY;
            PRAGMA count_changes = 0; /* Not required by our queries */
            PRAGMA cache_size = 128; /* Number of pages of 32KB */
            PRAGMA journal_size_limit = {0}; /* Size in bytes */
        ";

        public const string UpdateExpiry = @"
            update CacheItem
               set utcExpiry = strftime('%s', 'now') + interval
             where partition = @partition
               and key = @key
               and interval is not null
               and (utcExpiry is null or utcExpiry > strftime('%s', 'now')); -- Update only valid rows
        ";

        public const string Vacuum = @"vacuum; -- Clears free list and makes DB file smaller";
    }
}
