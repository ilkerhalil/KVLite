namespace KVLite

open System
open System.Collections.Concurrent
open System.Data
open System.Data.SqlServerCe
open System.Linq
open Dapper

/// <summary>
///   TODO
/// </summary>
[<Sealed>]
type private CacheContext(connection: IDbConnection, connectionString: string) =

    static let _connectionPool = new ConcurrentDictionary<string, ConcurrentStack<IDbConnection>>()

    static let getOrCreateConnection connectionString =
        let createNewConnection() =
            let connection = new SqlCeConnection(connectionString) :> IDbConnection
            connection.Open()
            connection

        let getCachedConnection() =
            let mutable (connectionList: ConcurrentStack<IDbConnection>) = null
            _connectionPool.TryGetValue(connectionString, ref connectionList) |> ignore
            match connectionList with
            | null -> createNewConnection()
            | _ when connectionList.Count = 0 -> createNewConnection()
            | _ -> let mutable connection: IDbConnection = null
                   if connectionList.TryPop(ref connection) then connection else createNewConnection()

        if _connectionPool.ContainsKey(connectionString) then getCachedConnection() else createNewConnection()

    static let tryCacheConnection connectionString connection =
        let addFirstList() = 
            let connectionList = new ConcurrentStack<IDbConnection>()
            connectionList.Push(connection)
            _connectionPool.TryAdd(connectionString, connectionList)
        
        let tryStoreConnection() =
            let mutable (connectionList: ConcurrentStack<IDbConnection>) = null
            _connectionPool.TryGetValue(connectionString, ref connectionList) |> ignore
            match connectionList with
            | null -> addFirstList()
            | _ when connectionList.Count <= Settings.MaxCachedConnectionCount -> connectionList.Push(connection); true 
            | _ -> false          

        if _connectionPool.ContainsKey(connectionString) then tryStoreConnection() else addFirstList()
            
    static member Create(connectionString) = new CacheContext(getOrCreateConnection connectionString, connectionString)

    member x.Connection = connection

    member x.Exists (query, args) =
        connection.Query<int>(query, args).Count() > 0
    
    member x.Exists (query: string) =
        connection.Query<int>(query).Count() > 0

    member x.Dispose disposing =
        if disposing && not (tryCacheConnection connectionString connection) then 
            connection.Dispose()

    interface IDisposable with
        member x.Dispose () =
            x.Dispose true
            GC.SuppressFinalize x

