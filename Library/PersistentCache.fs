namespace KVLite

open System
open System.Data.SqlServerCe
open System.IO
open System.Linq
open System.Runtime.Serialization.Formatters.Binary
open System.Web
open System.Web.Caching
open Dapper
open Thrower

type private clearParams = {ignoreExpirationDate: bool; utcNow: DateTime}
type private getParams = {partition: string; key: string; utcNow: DateTime}

/// <summary>
///   TODO
/// </summary>
[<Sealed>]
type public PersistentCache(cachePath) =
    inherit OutputCacheProvider()
    
    static let nullable = System.Nullable ()
    static let toNullable x = System.Nullable x

    static let Deserialize (array: byte[]) =
        let formatter = new BinaryFormatter()
        use stream = new MemoryStream(array)
        formatter.Deserialize(stream)

    static let Serialize value =
        let formatter = new BinaryFormatter()
        use stream = new MemoryStream()
        formatter.Serialize(stream, value)
        stream.ToArray()

    static let MapCachePath (path: string) =
        Raise<ArgumentException>.IfIsEmpty(path, ErrorMessages.NullOrEmptyCachePath)
        match HttpContext.Current with
        | null -> path
        | _ -> HttpContext.Current.Server.MapPath(path)      

    let mappedCachePath = MapCachePath cachePath
    let connectionString = String.Format(Settings.ConnectionStringFormat, mappedCachePath, Configuration.Instance.MaxCacheSize)
    let mutable defaultInstance = None   

    let DoAdd (partition: string) (key: string) value utcExpiry interval =
        Raise<ArgumentException>.IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition)
        Raise<ArgumentException>.IfIsEmpty(key, ErrorMessages.NullOrEmptyKey)

        let formattedValue = Serialize value

        use ctx = CacheContext.Create(connectionString)
        use trx = ctx.Connection.BeginTransaction()

        try
            let query = "select * from [cache_item] where [partition] = :partition and [key] = :key"
            let item = (ctx.Connection.Query<CacheItem> query).FirstOrDefault ()
            if item <> null then
                item.Value <- formattedValue
                item.Expiry <- utcExpiry
                item.Interval <- interval
                let update = """
                    update [cache_item]
                       set [value] = :Value
                         , [expiry] = :Expiry
                         , [interval] = :Interval
                     where [partition] = :Partition
                       and [key] = :Key
                """
                ctx.Connection.Execute(update) |> ignore
            else
                let insert = """
                    insert into [cache_item] ([partition], [key], [value], [expiry], [interval])
                    values (@partition, @key, @value, @expiry, @interval)
                """
                ctx.Connection.Execute(insert) |> ignore

            trx.Commit()
        with
            | ex -> trx.Rollback(); raise ex

        // Value is returned
        value
    
    do
        let cacheExists = File.Exists(mappedCachePath)
        if not cacheExists then
            let engine = new SqlCeEngine(connectionString)
            engine.CreateDatabase()

        use ctx = CacheContext.Create(connectionString)
        use trx = ctx.Connection.BeginTransaction()
            
        try
            let query = """
                select 1
                  from INFORMATION_SCHEMA.TABLES
                 where table_name = 'Cache_Item'
            """                  
            let cacheReady = ctx.Exists query
            if not cacheReady then
                ctx.Connection.Execute(Settings.CacheCreationScript) |> ignore
            
            trx.Commit()
        with
        | ex -> trx.Rollback(); raise ex    

    new() = PersistentCache(Configuration.Instance.CachePath)

    member x.Default: PersistentCache = 
        match defaultInstance with
        | None -> defaultInstance <- Some(new PersistentCache()); defaultInstance.Value
        | Some(_) -> defaultInstance.Value

    member x.AddPersistent (partition: string, key: string, value) =
        DoAdd partition key value nullable nullable

    member x.AddPersistent (key: string, value) =
        x.AddPersistent (Settings.DefaultPartition, key, value)

    member x.AddSliding (partition: string, key: string, value, interval: TimeSpan) =
        DoAdd partition key value (toNullable DateTime.UtcNow) (toNullable interval)
    
    member x.AddSliding (key: string, value, interval: TimeSpan) =
        x.AddSliding (Settings.DefaultPartition, key, value, interval)

    member x.AddTimed (partition: string, key: string, value, utcExpiry: DateTime) =
        DoAdd partition key value (toNullable utcExpiry) nullable
    
    member x.AddTimed (key: string, value, utcExpiry: DateTime) =
        x.AddTimed (Settings.DefaultPartition, key, value, utcExpiry)

    override x.Add (key, value, utcExpiry) =
        x.AddTimed (Settings.DefaultPartition, key, value, utcExpiry)

    member x.Clear (ignoreExpirationDate: bool) =
        let clearCmd = """
            delete from [cache_item]
             where @ignoreExpirationDate
                or ([expiry] is not null and [expiry] <= @utcNow)
        """
        use ctx = CacheContext.Create(connectionString)
        ctx.Connection.Execute(clearCmd, {ignoreExpirationDate = ignoreExpirationDate; utcNow = DateTime.UtcNow})
      
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Contains (partition: string, key: string) =
        let select = """
            select 1
              from [cache_item]
             where [partition] = @partition
               and [key] = @key
               and ([expiry] is null or [expiry] > @utcNow)
        """
        let args = {partition = partition; key = key; utcNow = DateTime.UtcNow}
        use ctx = CacheContext.Create(connectionString)    
        ctx.Exists (select, args)
    
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Contains key = x.Contains(Settings.DefaultPartition, key)

    /// <summary>
    ///   TODO
    /// </summary>
    member x.Get (partition: string, key: string) =
        let select = """
            select [value]
              from [cache_item]
             where [partition] = @partition
               and [key] = @key
               and ([expiry] is null or [expiry] > @utcNow)
        """
        let args = {partition = partition; key = key; utcNow = DateTime.UtcNow} 
        use ctx = CacheContext.Create(connectionString)    
        let item = ctx.Connection.Query<CacheItem>(select, args).FirstOrDefault()
        if item = null || item.Value = null then null else Deserialize item.Value
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Get key = x.Get(Settings.DefaultPartition, key)

    /// <summary>
    ///   TODO
    /// </summary>
    member x.GetInfo (partition: string, key: string) =
        let select = """
            select *
              from [cache_item]
             where [partition] = @partition
               and [key] = @key
               and [expiry] is null or [expiry] > @utcNow
        """
        let args = {partition = partition; key = key; utcNow = DateTime.UtcNow} 
        use ctx = CacheContext.Create connectionString
        let item = ctx.Connection.Query<CacheItem>(select, args).FirstOrDefault()
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
    member x.Remove (partition: string, key: string) =
        let delete = """
            delete from [cache_item]
             where [partition] = @partition
               and [key] = @key
        """
        let args = {partition = partition; key = key; utcNow = DateTime.Now}
        use ctx = CacheContext.Create(connectionString)
        ctx.Connection.Execute(delete, args) |> ignore
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Remove key = x.Remove (Settings.DefaultPartition, key)
    
    /// <summary>
    ///   TODO
    /// </summary>
    member x.Set (partition, key, value, utcExpiry) = x.AddTimed (partition, key, value, utcExpiry) |> ignore
    
    /// <summary>
    ///   TODO
    /// </summary>
    override x.Set (key, value, utcExpiry) = x.Set (Settings.DefaultPartition, key, value, utcExpiry)