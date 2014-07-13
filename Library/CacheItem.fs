namespace KVLite

open System
open System.Data.Linq.Mapping

/// <summary>
///   TODO
/// </summary>
[<Table(Name = "CACHE_ITEM")>]
[<AllowNullLiteral>]
[<Sealed>]
type private CacheItem() =
    
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