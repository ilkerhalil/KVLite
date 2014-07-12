namespace KVLite

open System.Collections.Concurrent
open System.Data.Common
open System.Data.SqlServerCe
open DbExtensions

/// <summary>
///   TODO
/// </summary>
type private CacheContext(connection: DbConnection, connectionString: string) =
    inherit Database(connection)

    static let _connectionPool = new ConcurrentDictionary<string, ConcurrentStack<DbConnection>>()

    static let getOrCreateConnection connectionString =
        let createNewConnection =
            let connection = SqlCeProviderFactory.Instance.CreateConnection(connectionString)
            connection.Open()
            connection

        let getCachedConnection =
            let mutable (connectionList: ConcurrentStack<DbConnection>) = null
            _connectionPool.TryGetValue(connectionString, ref connectionList) |> ignore
            match connectionList with
            | null -> createNewConnection
            | _ when connectionList.Count = 0 -> createNewConnection
            | _ -> let mutable connection: DbConnection = null
                   if connectionList.TryPop(ref connection) then connection else createNewConnection

        if _connectionPool.ContainsKey(connectionString) then getCachedConnection else createNewConnection

    static let tryCacheConnection connectionString connection =
        let addFirstList = 
            let connectionList = new ConcurrentStack<DbConnection>()
            connectionList.Push(connection)
            _connectionPool.TryAdd(connectionString, connectionList)
        
        let tryStoreConnection =
            let mutable (connectionList: ConcurrentStack<DbConnection>) = null
            _connectionPool.TryGetValue(connectionString, ref connectionList) |> ignore
            match connectionList with
            | null -> addFirstList
            | _ when connectionList.Count <= Settings.MaxCachedConnectionCount -> connectionList.Push(connection); true 
            | _ -> false          

        if _connectionPool.ContainsKey(connectionString) then tryStoreConnection else addFirstList
            

    static member Create(connectionString) = new CacheContext(getOrCreateConnection connectionString, connectionString)


    override x.Dispose(disposing) =
        if disposing && not (tryCacheConnection connectionString connection) then 
            connection.Dispose()
        base.Dispose(disposing)

