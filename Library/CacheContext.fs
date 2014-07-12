namespace KVLite

open System.Data.Common
open System.Data.SqlServerCe
open DbExtensions

/// <summary>
///   TODO
/// </summary>
type private CacheContext(connection: DbConnection, connectionString: string) =
    inherit Database(connection)

    static let mutable _connectionPool = Map.empty<string, list<DbConnection>>

    static let getOrCreateConnection connectionString =
        let createNewConnection =
            let connection = SqlCeProviderFactory.Instance.CreateConnection(connectionString)
            connection.Open()
            connection

        let getCachedConnection =
            let connectionList = Map.tryFind connectionString _connectionPool
            match connectionList with
            | None -> createNewConnection
            | Some([]) -> createNewConnection
            | Some(h::b) -> _connectionPool <- Map.remove connectionString _connectionPool |> Map.add connectionString b; h

        match Map.containsKey connectionString _connectionPool with
        | false -> createNewConnection
        | true -> getCachedConnection

    static let tryCacheConnection connectionString connection =
        let doStoreConnection connectionList =
            let newConnectionList = connection :: connectionList
            _connectionPool <- Map.remove connectionString _connectionPool |> Map.add connectionString newConnectionList
        
        let tryStoreConnection =
            let connectionList = Map.tryFind connectionString _connectionPool
            match connectionList with
            | None -> doStoreConnection []; true
            | Some(l) when List.length l = Settings.MaxCachedConnectionCount -> false 
            | Some(l) -> doStoreConnection l; true           

        match Map.containsKey connectionString _connectionPool with
        | false -> _connectionPool <- Map.add connectionString [connection] _connectionPool; true
        | true -> tryStoreConnection

    static member Create(connectionString) = new CacheContext(getOrCreateConnection connectionString, connectionString)

    override x.Dispose(disposing) =
        if disposing && not (tryCacheConnection connectionString connection) then 
            connection.Dispose()
        base.Dispose(disposing)

