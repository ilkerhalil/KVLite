namespace KVLite

open System.Configuration

module ConfigurationEntries =
    [<Literal>]
    let SectionName = "KVLiteConfiguration"
    
    [<Literal>] 
    let CachePath = "CachePath"

type public Configuration() =
    inherit ConfigurationSection()

    static let _instance = ConfigurationManager.GetSection(ConfigurationEntries.SectionName) :?> Configuration

    static member Instance = _instance
    
    [<ConfigurationProperty(ConfigurationEntries.CachePath, IsRequired = true)>]
    member x.CachePath = x.[ConfigurationEntries.CachePath] :?> string

module Settings =
    let ConnectionStringFormat = "Data Source={0}; Persist Security Info=False;"
    let DefaultPartition = "*"
    let CacheCreationScript = "CREATE TABLE [Cache_Item] ( \n\
                                   [Partition] NVARCHAR(100) NOT NULL, \n\
                                   [Key] NVARCHAR(100) NOT NULL, \n\
                                   [Value] IMAGE NOT NULL, \n\
                                   [Expiry] DATETIME, \n\
                                   [Interval] BIGINT, \n\
                                   CONSTRAINT Cache_Item_PK PRIMARY KEY ([Partition], [Key]) \n\
                               );"

module ErrorMessages =
    let NullOrEmptyCachePath = "Cache path cannot be null or empty."
    let NullOrEmptyKey = "Key cannot be null or empty."
    let NullOrEmptyPartition = "Partition cannot be null or empty."