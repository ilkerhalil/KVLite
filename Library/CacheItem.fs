namespace KVLite

open System

/// <summary>
///   TODO
/// </summary>
[<AllowNullLiteral; Sealed>]
type internal CacheItem public () =
    
    member val Partition = String.Empty with get, set
    member val Key = String.Empty with get, set
    member val Value = Array.empty<byte> with get, set
    member val Expiry = new Nullable<DateTime> () with get, set
    member val Interval = new Nullable<TimeSpan> () with get, set

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