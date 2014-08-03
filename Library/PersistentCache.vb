'
' PersistentCache.vb
' 
' Author(s):
'     Alessio Parma <alessio.parma@gmail.com>
'
' The MIT License (MIT)
' 
' Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
' 
' Permission is hereby granted, free of charge, to any person obtaining a copy
' of this software and associated documentation files (the "Software"), to deal
' in the Software without restriction, including without limitation the rights
' to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
' copies of the Software, and to permit persons to whom the Software is
' furnished to do so, subject to the following conditions:
' 
' The above copyright notice and this permission notice shall be included in
' all copies or substantial portions of the Software.
' 
' THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
' IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
' FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
' AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
' LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
' OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
' THE SOFTWARE.

Imports System.IO
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.Web
Imports System.Web.Caching
Imports Microsoft.VisualBasic.CompilerServices
Imports Dapper
Imports KVLite.My.Resources
Imports System.Data.SqlServerCe
Imports System.Linq
Imports Thrower

''' <summary>
'''   TODO
''' </summary>
Public NotInheritable Class PersistentCache
    Inherits OutputCacheProvider

    Private Shared _cachedDefaultInstance As PersistentCache

    Private ReadOnly _formatter As BinaryFormatter = New BinaryFormatter()
    Private ReadOnly _connectionString As String

    ' This value is increased for each ADD operation; after this value reaches the "OperationCountBeforeSoftClear"
    ' configuration parameter, then we must reset it and do a SOFT cleanup.
    Dim _operationCount As Short = 0

    Public Sub New(cachePath As String)
        Dim mappedCachePath = MapCachePath(cachePath)
        Me._connectionString = String.Format(My.Settings.ConnectionStringFormat, mappedCachePath, Configuration.Instance.MaxCacheSizeInMB)

        If Not File.Exists(mappedCachePath) Then
            Dim engine = New SqlCeEngine(Me._connectionString)
            engine.CreateDatabase()
        End If

        Using ctx = CacheContext.Create(Me._connectionString)
            Dim trx = ctx.Connection.BeginTransaction()
            Try
                If Not ctx.Exists(Queries.SchemaIsReady) Then
                    ctx.Connection.Execute(Queries.CacheSchema)
                    ctx.Connection.Execute(Queries.IndexSchema)
                End If
                trx.Commit()
            Catch
                trx.Rollback()
                Throw
            End Try
        End Using
    End Sub

    Public Sub New()
        Me.New(Configuration.Instance.DefaultCachePath)
    End Sub

    Public Shared ReadOnly Property DefaultInstance As PersistentCache
        Get
            If _cachedDefaultInstance Is Nothing Then _cachedDefaultInstance = New PersistentCache()
            Return _cachedDefaultInstance
        End Get
    End Property

#Region "Public Methods"

    Public Function AddPersistent(partition As String, key As String, value As Object) As Object
        Return DoAdd(partition, key, value, Nothing, Nothing)
    End Function

    Public Function AddPersistent(key As String, value As Object) As Object
        Return AddPersistent(My.Settings.DefaultPartition, key, value)
    End Function

    Public Function AddSliding(partition As String, key As String, value As Object, interval As TimeSpan) As Object
        Return DoAdd(partition, key, value, Date.UtcNow + interval, interval)
    End Function

    Public Function AddSliding(key As String, value As Object, interval As TimeSpan) As Object
        Return AddSliding(My.Settings.DefaultPartition, key, value, interval)
    End Function

    Public Function AddTimed(partition As String, key As String, value As Object, utcExpiry As Date) As Object
        Return DoAdd(partition, key, value, utcExpiry, Nothing)
    End Function

    Public Function AddTimed(key As String, value As Object, utcExpiry As Date) As Object
        Return AddTimed(My.Settings.DefaultPartition, key, value, utcExpiry)
    End Function

    Public Overrides Function Add(key As String, value As Object, utcExpiry As Date) As Object
        Return AddTimed(My.Settings.DefaultPartition, key, value, utcExpiry)
    End Function

    Public Sub Clear(clearMode As CacheClearMode)
        DoClear(clearMode)
    End Sub

    Public Sub Clear()
        Clear(CacheClearMode.IgnoreExpirationDate)
    End Sub

    Public Function Contains(partition As String, key As String) As Boolean
        Return DoGet(partition, key) IsNot Nothing
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Function Contains(key As String) As Boolean
        Return Contains(My.Settings.DefaultPartition, key)
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Overloads Function [Get](partition As String, key As String) As Object
        Dim item = DoGet(partition, key)
        Return If(item Is Nothing OrElse item.EncodedValue Is Nothing, Nothing, Deserialize(item.EncodedValue))
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Overrides Function [Get](ByVal key As String) As Object
        Return [Get](My.Settings.DefaultPartition, key)
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Function GetItem(partition As String, key As String) As ItemInfo
        Dim item = DoGet(partition, key)
        Return If(item Is Nothing OrElse item.EncodedValue Is Nothing, Nothing, New ItemInfo(item, Deserialize(item.EncodedValue)))
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Function GetItem(key As String) As ItemInfo
        Return GetItem(My.Settings.DefaultPartition, key)
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Function GetItems() As IEnumerable(Of ItemInfo)
        Using ctx = CacheContext.Create(Me._connectionString)
            Dim items = ctx.Connection.Query(Of CacheItem)(Queries.GetItems_Select, New With {Date.UtcNow})
            Return items.Select(Function(i) New ItemInfo(i, Deserialize(i.EncodedValue)))
        End Using
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Function GetItems(partition As String) As IEnumerable(Of ItemInfo)
        Using ctx = CacheContext.Create(Me._connectionString)
            Dim items = ctx.Connection.Query(Of CacheItem)(Queries.GetItems_SelectPartition, New With {partition, Date.UtcNow})
            Return items.Select(Function(i) New ItemInfo(i, Deserialize(i.EncodedValue)))
        End Using
    End Function

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Overloads Sub Remove(partition As String, key As String)
        Using ctx = CacheContext.Create(Me._connectionString)
            ctx.Connection.Execute(Queries.Remove, New With {partition, [key]})
        End Using
    End Sub

    ''' <summary>
    '''   TODO
    ''' </summary>
    Public Overrides Sub Remove(key As String)
        Remove(My.Settings.DefaultPartition, key)
    End Sub

    Public Overloads Sub [Set](partition As String, key As String, value As Object, utcExpiry As Date)
        AddTimed(partition, key, value, utcExpiry)
    End Sub

    Public Overrides Sub [Set](key As String, value As Object, utcExpiry As Date)
        AddTimed(My.Settings.DefaultPartition, key, value, utcExpiry)
    End Sub

#End Region

#Region "Private Methods"

    Private Function DoAdd(partition As String, key As String, value As Object, utcExpiry As Date?, interval As TimeSpan?) As Object
        Raise(Of ArgumentException).IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition)
        Raise(Of ArgumentException).IfIsEmpty(key, ErrorMessages.NullOrEmptyKey)

        Dim encodedValue = Serialize(value)
        Dim expiry = If(utcExpiry.HasValue, TryCast(utcExpiry.Value, Object), DBNull.Value)
        Dim ticks = If(interval.HasValue, TryCast(interval.Value.Ticks, Object), DBNull.Value)

        Using ctx = CacheContext.Create(Me._connectionString)
            Dim trx = ctx.Connection.BeginTransaction()
            Try
                Dim args = New With {partition, [key]}
                Dim item = ctx.Connection.Query(Of CacheItem)(Queries.DoAdd_Select, args).FirstOrDefault()

                Dim cmd = ctx.Connection.CreateCommand()
                cmd.CommandType = CommandType.Text
                cmd.CommandText = If(item Is Nothing, Queries.DoAdd_Insert, Queries.DoAdd_Update)
                cmd.Parameters.AddWithValue("Partition", partition).SqlDbType = SqlDbType.NVarChar
                cmd.Parameters.AddWithValue("Key", key).SqlDbType = SqlDbType.NVarChar
                cmd.Parameters.AddWithValue("EncodedValue", encodedValue).SqlDbType = SqlDbType.Image
                cmd.Parameters.AddWithValue("UtcCreation", DateTime.UtcNow).SqlDbType = SqlDbType.DateTime
                cmd.Parameters.AddWithValue("UtcExpiry", expiry).SqlDbType = SqlDbType.DateTime
                cmd.Parameters.AddWithValue("Interval", ticks).SqlDbType = SqlDbType.BigInt
                cmd.ExecuteNonQuery()

                ' Commit must be the _last_ instruction in the try block.
                trx.Commit()
            Catch
                trx.Rollback()
                Throw
            End Try
        End Using

        ' Operation has concluded successfully, therefore we increment the operation counter.
        ' If it has reached the "OperationCountBeforeSoftClear" configuration parameter,
        ' then we must reset it and do a SOFT cleanup.
        ' Following code is not fully thread safe, but it does not matter, because the
        ' "OperationCountBeforeSoftClear" parameter should be just an hint on when to do the cleanup.
        Me._operationCount = Me._operationCount + 1S
        If Me._operationCount = Configuration.Instance.OperationCountBeforeSoftCleanup Then
            Me._operationCount = 0
            DoClear(CacheClearMode.ConsiderExpirationDate)
        End If

        ' Value is returned
        Return value
    End Function

    Private Sub DoClear(clearMode As CacheClearMode)
        Dim ignoreExpirationDate = (clearMode = CacheClearMode.IgnoreExpirationDate)
        Using ctx = CacheContext.Create(Me._connectionString)
            ctx.Connection.Execute(Queries.DoClear, New With {ignoreExpirationDate, Date.UtcNow})
        End Using
    End Sub

    Private Function DoGet(partition As String, key As String) As CacheItem
        Raise(Of ArgumentException).IfIsEmpty(partition, ErrorMessages.NullOrEmptyPartition)
        Raise(Of ArgumentException).IfIsEmpty(key, ErrorMessages.NullOrEmptyKey)

        ' For this kind of task, we need a transaction. In fact, since the value may be sliding,
        ' we may have to issue an update following the initial select.
        Using ctx = CacheContext.Create(Me._connectionString)
            Dim trx = ctx.Connection.BeginTransaction()
            Try
                Dim args = New With {partition, [key], Date.UtcNow}
                Dim item = ctx.Connection.Query(Of CacheItem)(Queries.DoGet_Select, args).FirstOrDefault()
                If item IsNot Nothing AndAlso item.Interval.HasValue Then
                    ' Since item exists and it is sliding, then we need to update its expiration time.
                    item.UtcExpiry = item.UtcExpiry + TimeSpan.FromTicks(item.Interval.Value)
                    ctx.Connection.Execute(Queries.DoGet_UpdateExpiry, item)
                End If
                ' Commit must be the _last_ instruction in the try block, except for return.
                trx.Commit()
                ' We return the item we just found (or null if it does not exist).
                Return item
            Catch
                trx.Rollback()
                Throw
            End Try
        End Using
    End Function

    Private Shared Function MapCachePath(path As String) As String
        Raise(Of ArgumentException).IfIsEmpty(path, ErrorMessages.NullOrEmptyCachePath)
        Return If(HttpContext.Current Is Nothing, path, HttpContext.Current.Server.MapPath(path))
    End Function

#End Region

#Region "Serialization"

    Private Function Deserialize(encodedValue As Byte()) As Object
        Using stream = New MemoryStream(encodedValue)
            Return Me._formatter.Deserialize(stream)
        End Using
    End Function

    Private Function Serialize(value As Object) As Byte()
        Using stream = New MemoryStream()
            Me._formatter.Serialize(stream, value)
            Return stream.ToArray()
        End Using
    End Function

#End Region
End Class

Public Enum CacheClearMode
    IgnoreExpirationDate
    ConsiderExpirationDate
End Enum