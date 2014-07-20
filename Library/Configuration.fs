namespace KVLite

open System.Configuration

/// <summary>
///   TODO
/// </summary>
module private ConfigurationEntries =
    [<Literal>]
    let SectionName = "KVLiteConfiguration"
    
    [<Literal>] 
    let CachePath = "CachePath"

    [<Literal>]
    let MaxCacheSize = "MaxCacheSize"

/// <summary>
///   TODO
/// </summary>
[<Sealed>]
type public Configuration private() =
    inherit ConfigurationSection()

    static let _instance = ConfigurationManager.GetSection(ConfigurationEntries.SectionName) :?> Configuration

    static member Instance = _instance
    
    /// <summary>
    ///   TODO
    /// </summary>
    [<ConfigurationProperty(ConfigurationEntries.CachePath, IsRequired = true)>]
    member x.CachePath = x.[ConfigurationEntries.CachePath] :?> string
    
    /// <summary>
    ///   TODO
    /// </summary>
    [<ConfigurationProperty(ConfigurationEntries.MaxCacheSize, IsRequired = true)>]
    member x.MaxCacheSize = x.[ConfigurationEntries.MaxCacheSize] :?> int

/// <summary>
///   TODO
/// </summary>
module private Settings =
    let ConnectionStringFormat = "Data Source={0}; Max Database Size={1}; Persist Security Info=False;"
    let DefaultPartition = "*"
    let MaxCachedConnectionCount = 10
    let CacheCreationScript = """
        CREATE TABLE [Cache_Item] (
            [Partition] NVARCHAR(100) NOT NULL,
            [Key] NVARCHAR(100) NOT NULL,
            [Value] IMAGE NOT NULL,
            [Expiry] DATETIME,
            [Interval] BIGINT,
            CONSTRAINT Cache_Item_PK PRIMARY KEY ([Partition], [Key])
        );"""

/// <summary>
///   TODO
/// </summary>
module public ErrorMessages =
    let NullOrEmptyCachePath = "Cache path cannot be null or empty."
    let NullOrEmptyKey = "Key cannot be null or empty."
    let NullOrEmptyPartition = "Partition cannot be null or empty."