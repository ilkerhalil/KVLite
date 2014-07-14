namespace KVLite

open System
open System.Data.Linq.Mapping

/// <summary>
///   TODO
/// </summary>
[<Table(Name = "CACHE_ITEM"); AllowNullLiteral; Sealed>]
type internal CacheItem public () =
    
    [<Column(IsPrimaryKey = true)>]
    member val Partition = String.Empty with get, set

    [<Column(IsPrimaryKey = true)>]
    member val Key = String.Empty with get, set

    [<Column(CanBeNull = false, DbType = "Image")>]
    member val Value = Array.empty<byte> with get, set

    [<Column(CanBeNull = true)>]
    member val Expiry = new Nullable<DateTime>() with get, set

    [<Column(CanBeNull = true)>]
    member val Interval = new Nullable<TimeSpan>() with get, set

/// <summary>
///   TODO
/// </summary>
[<AllowNullLiteral; Sealed>]
type public ItemInfo internal (partition, key, value, expiry, interval) =
    
    internal new (ci: CacheItem, v: obj) = ItemInfo (ci.Partition, ci.Key, v, ci.Expiry, ci.Interval)

    member x.Partition = partition
    member x.Key = key
    member x.Value = value
    member x.Expiry = expiry
    member x.Interval = interval