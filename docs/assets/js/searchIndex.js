
var camelCaseTokenizer = function (obj) {
    var previous = '';
    return obj.toString().trim().split(/[\s\-]+|(?=[A-Z])/).reduce(function(acc, cur) {
        var current = cur.toLowerCase();
        if(acc.length === 0) {
            previous = current;
            return acc.concat(current);
        }
        previous = previous.concat(current);
        return acc.concat([current, previous]);
    }, []);
}
lunr.tokenizer.registerFunction(camelCaseTokenizer, 'camelCaseTokenizer')
var searchModule = function() {
    var idMap = [];
    function y(e) { 
        idMap.push(e); 
    }
    var idx = lunr(function() {
        this.field('title', { boost: 10 });
        this.field('content');
        this.field('description', { boost: 5 });
        this.field('tags', { boost: 50 });
        this.ref('id');
        this.tokenizer(camelCaseTokenizer);

        this.pipeline.remove(lunr.stopWordFilter);
        this.pipeline.remove(lunr.stemmer);
    });
    function a(e) { 
        idx.add(e); 
    }

    a({
        id:0,
        title:"CacheItem",
        content:"CacheItem",
        description:'',
        tags:''
    });

    a({
        id:1,
        title:"BlobSerializer",
        content:"BlobSerializer",
        description:'',
        tags:''
    });

    a({
        id:2,
        title:"NoOpCacheSettings",
        content:"NoOpCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:3,
        title:"ServiceCollectionExtensions",
        content:"ServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:4,
        title:"NoOpCache",
        content:"NoOpCache",
        description:'',
        tags:''
    });

    a({
        id:5,
        title:"WebCaches",
        content:"WebCaches",
        description:'',
        tags:''
    });

    a({
        id:6,
        title:"ClaimConverter",
        content:"ClaimConverter",
        description:'',
        tags:''
    });

    a({
        id:7,
        title:"ViewStateStorageMethod",
        content:"ViewStateStorageMethod",
        description:'',
        tags:''
    });

    a({
        id:8,
        title:"SqlServerServiceCollectionExtensions",
        content:"SqlServerServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:9,
        title:"ViewStateStorageSettings",
        content:"ViewStateStorageSettings",
        description:'',
        tags:''
    });

    a({
        id:10,
        title:"PooledMemoryStream",
        content:"PooledMemoryStream",
        description:'',
        tags:''
    });

    a({
        id:11,
        title:"IAsyncCacheSettings",
        content:"IAsyncCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:12,
        title:"AbstractOutputCacheProvider",
        content:"AbstractOutputCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:13,
        title:"DbCacheSettings",
        content:"DbCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:14,
        title:"IEssentialCache",
        content:"IEssentialCache",
        description:'',
        tags:''
    });

    a({
        id:15,
        title:"CachingEnumerable",
        content:"CachingEnumerable",
        description:'',
        tags:''
    });

    a({
        id:16,
        title:"MemoryServiceCollectionExtensions",
        content:"MemoryServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:17,
        title:"ICacheItem",
        content:"ICacheItem",
        description:'',
        tags:''
    });

    a({
        id:18,
        title:"CacheExtensions",
        content:"CacheExtensions",
        description:'',
        tags:''
    });

    a({
        id:19,
        title:"IAsyncCache",
        content:"IAsyncCache",
        description:'',
        tags:''
    });

    a({
        id:20,
        title:"CacheReadMode",
        content:"CacheReadMode",
        description:'',
        tags:''
    });

    a({
        id:21,
        title:"OutputCacheProvider",
        content:"OutputCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:22,
        title:"PostgreSqlCacheSettings",
        content:"PostgreSqlCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:23,
        title:"MySqlServiceCollectionExtensions",
        content:"MySqlServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:24,
        title:"QueryCacheProvider",
        content:"QueryCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:25,
        title:"SqlServerCache",
        content:"SqlServerCache",
        description:'',
        tags:''
    });

    a({
        id:26,
        title:"CacheResult",
        content:"CacheResult",
        description:'',
        tags:''
    });

    a({
        id:27,
        title:"SQLiteCacheConnectionFactory",
        content:"SQLiteCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:28,
        title:"NoOpCompressor NoOpStream",
        content:"NoOpCompressor NoOpStream",
        description:'',
        tags:''
    });

    a({
        id:29,
        title:"IRandom",
        content:"IRandom",
        description:'',
        tags:''
    });

    a({
        id:30,
        title:"PersistentOutputCacheProvider",
        content:"PersistentOutputCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:31,
        title:"IEssentialCacheSettings",
        content:"IEssentialCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:32,
        title:"MemoryServiceCollectionExtensions",
        content:"MemoryServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:33,
        title:"VolatileCache",
        content:"VolatileCache",
        description:'',
        tags:''
    });

    a({
        id:34,
        title:"NoOpCompressor",
        content:"NoOpCompressor",
        description:'',
        tags:''
    });

    a({
        id:35,
        title:"VolatileCacheSettings",
        content:"VolatileCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:36,
        title:"SystemRandom",
        content:"SystemRandom",
        description:'',
        tags:''
    });

    a({
        id:37,
        title:"DeflateCompressor",
        content:"DeflateCompressor",
        description:'',
        tags:''
    });

    a({
        id:38,
        title:"ICache",
        content:"ICache",
        description:'',
        tags:''
    });

    a({
        id:39,
        title:"MemoryOutputCacheProvider",
        content:"MemoryOutputCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:40,
        title:"ViewStateStorageBehavior",
        content:"ViewStateStorageBehavior",
        description:'',
        tags:''
    });

    a({
        id:41,
        title:"IAsyncCache",
        content:"IAsyncCache",
        description:'',
        tags:''
    });

    a({
        id:42,
        title:"AsyncCacheExtensions",
        content:"AsyncCacheExtensions",
        description:'',
        tags:''
    });

    a({
        id:43,
        title:"AbstractViewStatePersister",
        content:"AbstractViewStatePersister",
        description:'',
        tags:''
    });

    a({
        id:44,
        title:"BinarySerializerSettings",
        content:"BinarySerializerSettings",
        description:'',
        tags:''
    });

    a({
        id:45,
        title:"PersistentCacheSettings",
        content:"PersistentCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:46,
        title:"PersistentCache",
        content:"PersistentCache",
        description:'',
        tags:''
    });

    a({
        id:47,
        title:"FireAndForget",
        content:"FireAndForget",
        description:'',
        tags:''
    });

    a({
        id:48,
        title:"SqlServerCacheConnectionFactory",
        content:"SqlServerCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:49,
        title:"ICompressor",
        content:"ICompressor",
        description:'',
        tags:''
    });

    a({
        id:50,
        title:"Hashing",
        content:"Hashing",
        description:'',
        tags:''
    });

    a({
        id:51,
        title:"DbCacheEntry Single",
        content:"DbCacheEntry Single",
        description:'',
        tags:''
    });

    a({
        id:52,
        title:"BinarySerializer",
        content:"BinarySerializer",
        description:'',
        tags:''
    });

    a({
        id:53,
        title:"Logger",
        content:"Logger",
        description:'',
        tags:''
    });

    a({
        id:54,
        title:"AbstractCacheController",
        content:"AbstractCacheController",
        description:'',
        tags:''
    });

    a({
        id:55,
        title:"ILogProvider",
        content:"ILogProvider",
        description:'',
        tags:''
    });

    a({
        id:56,
        title:"MySqlCacheSettings",
        content:"MySqlCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:57,
        title:"PostgreSqlCache",
        content:"PostgreSqlCache",
        description:'',
        tags:''
    });

    a({
        id:58,
        title:"DbCacheEntry Group",
        content:"DbCacheEntry Group",
        description:'',
        tags:''
    });

    a({
        id:59,
        title:"OracleCacheSettings",
        content:"OracleCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:60,
        title:"CachePartitions",
        content:"CachePartitions",
        description:'',
        tags:''
    });

    a({
        id:61,
        title:"MySqlCacheConnectionFactory",
        content:"MySqlCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:62,
        title:"LogProvider",
        content:"LogProvider",
        description:'',
        tags:''
    });

    a({
        id:63,
        title:"AntiTamper IObjectWithHashCode",
        content:"AntiTamper IObjectWithHashCode",
        description:'',
        tags:''
    });

    a({
        id:64,
        title:"VolatileOutputCacheProvider",
        content:"VolatileOutputCacheProvider",
        description:'',
        tags:''
    });

    a({
        id:65,
        title:"MemoryCache",
        content:"MemoryCache",
        description:'',
        tags:''
    });

    a({
        id:66,
        title:"LogLevel",
        content:"LogLevel",
        description:'',
        tags:''
    });

    a({
        id:67,
        title:"AbstractCache",
        content:"AbstractCache",
        description:'',
        tags:''
    });

    a({
        id:68,
        title:"ISerializer",
        content:"ISerializer",
        description:'',
        tags:''
    });

    a({
        id:69,
        title:"SQLiteCacheSettings",
        content:"SQLiteCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:70,
        title:"AntiTamper",
        content:"AntiTamper",
        description:'',
        tags:''
    });

    a({
        id:71,
        title:"OracleCache",
        content:"OracleCache",
        description:'',
        tags:''
    });

    a({
        id:72,
        title:"PersistentViewStatePersister",
        content:"PersistentViewStatePersister",
        description:'',
        tags:''
    });

    a({
        id:73,
        title:"ICache",
        content:"ICache",
        description:'',
        tags:''
    });

    a({
        id:74,
        title:"OracleServiceCollectionExtensions",
        content:"OracleServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:75,
        title:"AbstractCacheSettings",
        content:"AbstractCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:76,
        title:"DbCache",
        content:"DbCache",
        description:'',
        tags:''
    });

    a({
        id:77,
        title:"SQLiteServiceCollectionExtensions",
        content:"SQLiteServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:78,
        title:"DebugMessages",
        content:"DebugMessages",
        description:'',
        tags:''
    });

    a({
        id:79,
        title:"VolatileViewStatePersister",
        content:"VolatileViewStatePersister",
        description:'',
        tags:''
    });

    a({
        id:80,
        title:"MySqlCache",
        content:"MySqlCache",
        description:'',
        tags:''
    });

    a({
        id:81,
        title:"FakeRandom",
        content:"FakeRandom",
        description:'',
        tags:''
    });

    a({
        id:82,
        title:"GZipCompressor",
        content:"GZipCompressor",
        description:'',
        tags:''
    });

    a({
        id:83,
        title:"MemoryViewStatePersister",
        content:"MemoryViewStatePersister",
        description:'',
        tags:''
    });

    a({
        id:84,
        title:"DbCacheValue",
        content:"DbCacheValue",
        description:'',
        tags:''
    });

    a({
        id:85,
        title:"ErrorMessages",
        content:"ErrorMessages",
        description:'',
        tags:''
    });

    a({
        id:86,
        title:"OracleCacheConnectionFactory",
        content:"OracleCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:87,
        title:"NoOpServiceCollectionExtensions",
        content:"NoOpServiceCollectionExtensions",
        description:'',
        tags:''
    });

    a({
        id:88,
        title:"JsonSerializer",
        content:"JsonSerializer",
        description:'',
        tags:''
    });

    a({
        id:89,
        title:"StringUtils",
        content:"StringUtils",
        description:'',
        tags:''
    });

    a({
        id:90,
        title:"DbCacheConnectionFactory",
        content:"DbCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:91,
        title:"PostgreSqlCacheConnectionFactory",
        content:"PostgreSqlCacheConnectionFactory",
        description:'',
        tags:''
    });

    a({
        id:92,
        title:"DbCacheEntry",
        content:"DbCacheEntry",
        description:'',
        tags:''
    });

    a({
        id:93,
        title:"MemoryCacheSettings",
        content:"MemoryCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:94,
        title:"QueryCacheExtensions",
        content:"QueryCacheExtensions",
        description:'',
        tags:''
    });

    a({
        id:95,
        title:"ICacheSettings",
        content:"ICacheSettings",
        description:'',
        tags:''
    });

    a({
        id:96,
        title:"SqlServerCacheSettings",
        content:"SqlServerCacheSettings",
        description:'',
        tags:''
    });

    a({
        id:97,
        title:"NoOpSerializer",
        content:"NoOpSerializer",
        description:'',
        tags:''
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/CacheItem_1',
        title:"CacheItem<TVal>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/BlobSerializer',
        title:"BlobSerializer",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.NoOp/NoOpCacheSettings',
        title:"NoOpCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/ServiceCollectionExtensions',
        title:"ServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.NoOp/NoOpCache',
        title:"NoOpCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/WebCaches',
        title:"WebCaches",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core.Extensibility.Converters/ClaimConverter',
        title:"ClaimConverter",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/ViewStateStorageMethod',
        title:"ViewStateStorageMethod",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SqlServer/SqlServerServiceCollectionExtensions',
        title:"SqlServerServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/ViewStateStorageSettings',
        title:"ViewStateStorageSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/PooledMemoryStream',
        title:"PooledMemoryStream",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/IAsyncCacheSettings',
        title:"IAsyncCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/AbstractOutputCacheProvider',
        title:"AbstractOutputCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/DbCacheSettings_1',
        title:"DbCacheSettings<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/IEssentialCache',
        title:"IEssentialCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Goodies/CachingEnumerable_1',
        title:"CachingEnumerable<T>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Memory/MemoryServiceCollectionExtensions',
        title:"MemoryServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/ICacheItem_1',
        title:"ICacheItem<TVal>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/CacheExtensions',
        title:"CacheExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/IAsyncCache_1',
        title:"IAsyncCache<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/CacheReadMode',
        title:"CacheReadMode",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebApi/OutputCacheProvider',
        title:"OutputCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.PostgreSql/PostgreSqlCacheSettings',
        title:"PostgreSqlCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.MySql/MySqlServiceCollectionExtensions',
        title:"MySqlServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.EntityFramework/QueryCacheProvider',
        title:"QueryCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SqlServer/SqlServerCache',
        title:"SqlServerCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/CacheResult_1',
        title:"CacheResult<T>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/SQLiteCacheConnectionFactory_1',
        title:"SQLiteCacheConnectionFactory<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/NoOpStream',
        title:"NoOpCompressor.NoOpStream",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/IRandom',
        title:"IRandom",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/PersistentOutputCacheProvider',
        title:"PersistentOutputCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/IEssentialCacheSettings',
        title:"IEssentialCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.PostgreSql/MemoryServiceCollectionExtensions',
        title:"MemoryServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/VolatileCache',
        title:"VolatileCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/NoOpCompressor',
        title:"NoOpCompressor",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/VolatileCacheSettings',
        title:"VolatileCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/SystemRandom',
        title:"SystemRandom",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/DeflateCompressor',
        title:"DeflateCompressor",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/ICache',
        title:"ICache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/MemoryOutputCacheProvider',
        title:"MemoryOutputCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/ViewStateStorageBehavior',
        title:"ViewStateStorageBehavior",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/IAsyncCache',
        title:"IAsyncCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/AsyncCacheExtensions',
        title:"AsyncCacheExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/AbstractViewStatePersister',
        title:"AbstractViewStatePersister",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/BinarySerializerSettings',
        title:"BinarySerializerSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/PersistentCacheSettings',
        title:"PersistentCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/PersistentCache',
        title:"PersistentCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Goodies/FireAndForget',
        title:"FireAndForget",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SqlServer/SqlServerCacheConnectionFactory',
        title:"SqlServerCacheConnectionFactory",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/ICompressor',
        title:"ICompressor",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/Hashing',
        title:"Hashing",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/Single',
        title:"DbCacheEntry.Single",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/BinarySerializer',
        title:"BinarySerializer",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Logging/Logger',
        title:"Logger",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebApi/AbstractCacheController',
        title:"AbstractCacheController",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Logging/ILogProvider',
        title:"ILogProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.MySql/MySqlCacheSettings',
        title:"MySqlCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.PostgreSql/PostgreSqlCache',
        title:"PostgreSqlCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/Group',
        title:"DbCacheEntry.Group",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Oracle/OracleCacheSettings',
        title:"OracleCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Resources/CachePartitions',
        title:"CachePartitions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.MySql/MySqlCacheConnectionFactory',
        title:"MySqlCacheConnectionFactory",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Logging/LogProvider',
        title:"LogProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/IObjectWithHashCode64',
        title:"AntiTamper.IObjectWithHashCode64",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/VolatileOutputCacheProvider',
        title:"VolatileOutputCacheProvider",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Memory/MemoryCache',
        title:"MemoryCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Logging/LogLevel',
        title:"LogLevel",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/AbstractCache_2',
        title:"AbstractCache<TCache, TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/ISerializer',
        title:"ISerializer",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/SQLiteCacheSettings_1',
        title:"SQLiteCacheSettings<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/AntiTamper',
        title:"AntiTamper",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Oracle/OracleCache',
        title:"OracleCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/PersistentViewStatePersister',
        title:"PersistentViewStatePersister",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/ICache_1',
        title:"ICache<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Oracle/OracleServiceCollectionExtensions',
        title:"OracleServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/AbstractCacheSettings_1',
        title:"AbstractCacheSettings<TSettings>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/DbCache_4',
        title:"DbCache<TCache, TSettings, TConnectionFactory, TConnection>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SQLite/SQLiteServiceCollectionExtensions',
        title:"SQLiteServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Resources/DebugMessages',
        title:"DebugMessages",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/VolatileViewStatePersister',
        title:"VolatileViewStatePersister",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.MySql/MySqlCache',
        title:"MySqlCache",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/FakeRandom',
        title:"FakeRandom",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/GZipCompressor',
        title:"GZipCompressor",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.WebForms/MemoryViewStatePersister',
        title:"MemoryViewStatePersister",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/DbCacheValue',
        title:"DbCacheValue",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Resources/ErrorMessages',
        title:"ErrorMessages",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Oracle/OracleCacheConnectionFactory',
        title:"OracleCacheConnectionFactory",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.NoOp/NoOpServiceCollectionExtensions',
        title:"NoOpServiceCollectionExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/JsonSerializer',
        title:"JsonSerializer",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Core/StringUtils',
        title:"StringUtils",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/DbCacheConnectionFactory_2',
        title:"DbCacheConnectionFactory<TSettings, TConnection>",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.PostgreSql/PostgreSqlCacheConnectionFactory',
        title:"PostgreSqlCacheConnectionFactory",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Database/DbCacheEntry',
        title:"DbCacheEntry",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Memory/MemoryCacheSettings',
        title:"MemoryCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/EntityFramework.Extensions/QueryCacheExtensions',
        title:"QueryCacheExtensions",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite/ICacheSettings',
        title:"ICacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.SqlServer/SqlServerCacheSettings',
        title:"SqlServerCacheSettings",
        description:""
    });

    y({
        url:'/KVLite/api/PommaLabs.KVLite.Extensibility/NoOpSerializer',
        title:"NoOpSerializer",
        description:""
    });

    return {
        search: function(q) {
            return idx.search(q).map(function(i) {
                return idMap[i.ref];
            });
        }
    };
}();
