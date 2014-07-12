namespace KVLite

open System.Data.Common
open System.Data.SqlServerCe
open DbExtensions

type CacheContext(connection: DbConnection) =
    inherit Database(connection)

    static member Create(connectionString) =
        let connection = SqlCeProviderFactory.Instance.CreateConnection(connectionString)
        connection.Open()
        new CacheContext(connection)

    override x.Dispose(disposing) =
        if disposing then connection.Dispose()
        base.Dispose(disposing)

