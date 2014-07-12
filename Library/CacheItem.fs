namespace KVLite

open System
open System.Data.Linq.Mapping

/// <summary>
///   TODO
/// </summary>
[<Table(Name = "CACHE_ITEM")>]
[<AllowNullLiteral>]
type private CacheItem() =
    
    [<Column(IsPrimaryKey = true)>]
    member val Partition = "" with get, set

    [<Column(IsPrimaryKey = true)>]
    member val Key = "" with get, set

    [<Column(CanBeNull = false, DbType = "Image")>]
    member val Value = Array.empty<byte> with get, set

    [<Column(CanBeNull = true)>]
    member val Expiry = new DateTime() with get, set

    [<Column(CanBeNull = true)>]
    member val Interval = new TimeSpan() with get, set