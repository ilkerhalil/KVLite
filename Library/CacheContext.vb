'
' CacheContext.vb
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

Imports System.Collections.Concurrent
Imports System.Data.SqlServerCe
Imports System.Linq
Imports Dapper

Friend NotInheritable Class CacheContext
    Implements IDisposable    
    
    Private Shared ReadOnly ConnectionPool As ConcurrentDictionary(Of String, ConcurrentStack(Of IDbConnection)) = New ConcurrentDictionary(Of String, ConcurrentStack(Of IDbConnection))
    
    Private ReadOnly _connection As IDbConnection
    Private _disposed As Boolean = False
    
    #Region "Construction"
    
    Private Sub New(connection As IDbConnection)
        Me._connection = connection
    End Sub
    
    Public Shared Function Create(connectionString As String) As CacheContext
        Dim connection = GetOrCreateConnection(connectionString)
        Return New CacheContext(connection)
    End Function
    
    #End Region
    
    Public ReadOnly Property Connection As IDbConnection
        Get
            Return Me._connection
        End Get
    End Property
    
    Public Function Exists(query As String, args As Object) As Boolean
        Return Me._connection.Query(Of Integer)(query, args).Any()
    End Function
    
    Public Function Exists(query As String) As Boolean
        Return Me._connection.Query(Of Integer)(query).Any()
    End Function
    
    #Region "IDisposable Members"
    
    Public Sub Dispose() Implements IDisposable.Dispose
        If Me._disposed Then Return
        If Not TryCacheConnection(Me._connection) Then Me._connection.Dispose()
        GC.SuppressFinalize(Me)
        Me._disposed = True
    End Sub
    
    #End Region
    
    #Region "Connection Retrieval"
    
    Private Shared Function GetOrCreateConnection(connenctionString As String) As IDbConnection
        If ConnectionPool.ContainsKey(connenctionString) Then
            Return GetCachedConnection(connenctionString)
        Else
            Return CreateNewConnection(connenctionString)
        End If
    End Function
    
    Private Shared Function CreateNewConnection(connectionString As String) As IDbConnection
        Dim connection = New SqlCeConnection(connectionString)
        connection.Open()
        Return connection
    End Function
    
    Private Shared Function GetCachedConnection(connectionString As String) As IDbConnection
        Dim connectionList As ConcurrentStack(Of IDbConnection) = Nothing
        ConnectionPool.TryGetValue(connectionString, connectionList)
        If connectionList Is Nothing OrElse connectionList.Count = 0 Then Return CreateNewConnection(connectionString)
        Dim connection As IDbConnection = Nothing
        connectionList.TryPop(connection)
        Return If(connection Is Nothing, CreateNewConnection(connectionString), connection)
    End Function
    
    #End Region   
    
    #Region "Connection Caching"
    
    Private Shared Function  TryCacheConnection(connection As IDbConnection) As Boolean
        If ConnectionPool.ContainsKey(connection.ConnectionString) Then
            Return TryStoreConnection(connection)
        Else
            Return AddFirstList(connection)
        End If
    End Function
    
    Private Shared Function TryStoreConnection(connection As IDbConnection) As Boolean
        Dim connectionList As ConcurrentStack(Of IDbConnection) = Nothing
        ConnectionPool.TryGetValue(connection.ConnectionString, connectionList)
        Dim maxConnCount = Configuration.Instance.MaxCachedConnectionCount
        If connectionList Is Nothing Then Return AddFirstList(connection)
        If connectionList.Count <= maxConnCount Then
            connectionList.Push(connection)
            Return True
        End If
        Return False
    End Function
    
    Private Shared Function AddFirstList(connection As IDbConnection) As Boolean
        Dim connectionList = New ConcurrentStack(Of IDbConnection)
        connectionList.Push(connection)
        Return ConnectionPool.TryAdd(connection.ConnectionString, connectionList)
    End Function
    
    #End Region
    
End Class
