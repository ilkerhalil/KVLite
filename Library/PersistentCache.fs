namespace KVLite

open System
open System.IO
open System.Linq
open System.Runtime.Serialization.Formatters.Binary
open System.Web
open System.Web.Caching
open DbExtensions
open Thrower
open System.Data.SqlServerCe

/// <summary>
///   TODO
/// </summary>
[<Sealed>]
type public PersistentCache(cachePath) =
    inherit OutputCacheProvider()
    
    let MapCachePath (path: string) =
        Raise<ArgumentException>.IfIsEmpty(path, ErrorMessages.NullOrEmptyCachePath)
        match HttpContext.Current with
        | null -> path
        | _ -> HttpContext.Current.Server.MapPath(path)      

    let mappedCachePath = MapCachePath cachePath
    let connectionString = String.Format(Settings.ConnectionStringFormat, mappedCachePath)
    let mutable defaultInstance = None

    let Deserialize (array: byte[]) =
        let formatter = new BinaryFormatter()
        use stream = new MemoryStream(array)
        formatter.Deserialize(stream)

    let Serialize value =
        let formatter = new BinaryFormatter()
        use stream = new MemoryStream()
        formatter.Serialize(stream, value)
        stream.ToArray()

    let DoAdd (partition: string) (key: string) value utcExpiry interval =
        Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition)
        Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey)

        let formattedValue = Serialize value

        use ctx = CacheContext.Create(connectionString)
        use trx = ctx.Connection.BeginTransaction()

        try
            let query = (SQL
                        .SELECT("*")
                        .FROM("[CACHE_ITEM]")
                        .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key))

            if ctx.Exists(query) then
                let update = (SQL
                             .UPDATE("[CACHE_ITEM]")
                             .SET("[VALUE] = {0}, [EXPIRY] = {1}", SQL.Param(formattedValue), utcExpiry)
                             .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key))
                ctx.Execute(update) |> ignore
            else
                let insert = (SQL
                             .INSERT_INTO("[CACHE_ITEM]")
                             .VALUES(partition, key, formattedValue, utcExpiry, interval))
                ctx.Execute(insert) |> ignore

            trx.Commit()
        with
            | ex -> trx.Rollback(); raise ex

        // Value is returned
        value
    
    do
        let cacheExists = File.Exists(cachePath)
        if not cacheExists then
            let engine = new SqlCeEngine(connectionString)
            engine.CreateDatabase()

        use ctx = CacheContext.Create(connectionString)
        use trx = ctx.Connection.BeginTransaction()
            
        try
            let query = (SQL
                        .SELECT("1")
                        .FROM("INFORMATION_SCHEMA.TABLES")
                        .WHERE("TABLE_NAME = {0}", "Cache_Item"))
                    
            let cacheReady = ctx.Exists(query)
            if not cacheReady then
                ctx.Execute(Settings.CacheCreationScript) |> ignore
            
            trx.Commit()
        with
        | ex -> trx.Rollback(); raise ex    

    new() = PersistentCache(Configuration.Instance.CachePath)

    member x.Default = 
        match defaultInstance with
        | None -> defaultInstance <- Some(new PersistentCache()); defaultInstance.Value
        | Some(_) -> defaultInstance.Value

    member x.AddPersistent(partition, key, value) =
        DoAdd partition key value null null

    member x.AddPersistent(key, value) =
        x.AddPersistent(Settings.DefaultPartition, key, value)

    member x.AddSliding(partition, key, value, interval) =
        DoAdd partition key value DateTime.UtcNow interval
    
    member x.AddSliding(key, value, interval) =
        x.AddSliding(Settings.DefaultPartition, key, value, interval)

    member x.AddTimed(partition, key, value, utcExpiry) =
        DoAdd partition key value utcExpiry null
    
    member x.AddTimed(key, value, utcExpiry) =
        x.AddTimed(Settings.DefaultPartition, key, value, utcExpiry)

    override x.Add(key, value, utcExpiry) =
        x.AddTimed(Settings.DefaultPartition, key, value, utcExpiry)

    member x.Clear(ignoreExpirationDate) =
        let clearCmd = (SQL
                       .DELETE_FROM("[CACHE_ITEM]")
                       ._If(not ignoreExpirationDate, "[EXPIRY] IS NOT NULL AND [EXPIRY] <= {0}", DateTime.UtcNow))

        use ctx = CacheContext.Create(connectionString)
        ctx.Execute(clearCmd)
      
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Contains(partition, key) =
        let select = (SQL
                     .SELECT("1")
                     .FROM("[CACHE_ITEM]")
                     .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key)
                     .WHERE("([EXPIRY] IS NULL OR [EXPIRY] > {0})", DateTime.UtcNow))

        use ctx = CacheContext.Create(connectionString)    
        ctx.Exists(select)
    
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Contains(key) = x.Contains(Settings.DefaultPartition, key)

    /// <summary>
    ///   TODO
    /// </summary>
    member x.Get(partition, key) =
        let select = (SQL
                     .SELECT("[VALUE]")
                     .FROM("[CACHE_ITEM]")
                     .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key)
                     .WHERE("([EXPIRY] IS NULL OR [EXPIRY] > {0})", DateTime.UtcNow))
        
        use ctx = CacheContext.Create(connectionString)    
        let item = ctx.Map<CacheItem>(select).FirstOrDefault()
        if item = null || item.Value = null then null else Deserialize item.Value
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Get(key) = x.Get(Settings.DefaultPartition, key)

    /// <summary>
    ///   TODO
    /// </summary>
    member x.GetInfo (partition, key) =
        let select = (SQL
                     .SELECT("*")
                     .FROM("[CACHE_ITEM]")
                     .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key)
                     .WHERE("([EXPIRY] IS NULL OR [EXPIRY] > {0})", DateTime.UtcNow))
        use ctx = CacheContext.Create connectionString    
        let item = ctx.Map<CacheItem>(select).FirstOrDefault()
        if item = null || item.Value = null then 
            null // Item is not contained
        else 
            ItemInfo (item, Deserialize item.Value)

    /// <summary>
    ///   TODO
    /// </summary>
    member x.GetInfo key = x.GetInfo (Settings.DefaultPartition, key)
    
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Remove(partition, key) =
        let delete = (SQL
                     .DELETE_FROM("[CACHE_ITEM]")
                     .WHERE("[PARTITION] = {0} AND [KEY] = {1}", partition, key))

        use ctx = CacheContext.Create(connectionString)
        ctx.Execute(delete) |> ignore
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Remove(key) = x.Remove(Settings.DefaultPartition, key)
    
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Set(partition, key, value, utcExpiry) = x.AddTimed(partition, key, value, utcExpiry) |> ignore
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Set(key, value, utcExpiry) = x.Set(Settings.DefaultPartition, key, value, utcExpiry)