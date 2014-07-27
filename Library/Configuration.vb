'
' Configuration.vb
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

Imports System.Configuration

''' <summary>
'''   TODO
''' </summary>
Public NotInheritable Class Configuration
    Inherits ConfigurationSection
    
    Private Const SectionName As String = "KVLiteConfiguration"
    Private Const DefaultCachePathKey As String = "DefaultCachePath"
    Private Const MaxCacheSizeInMBKey As String = "MaxCacheSizeInMB"
    Private Const MaxCachedConnectionCountKey As String = "MaxCachedConnectionCount"
    Private Const OperationCountBeforeSoftCleanupKey As String = "OperationCountBeforeSoftCleanup"
    
    Private Shared ReadOnly CachedInstance As Configuration = CType(ConfigurationManager.GetSection(SectionName), Configuration)
    
    Public Shared ReadOnly Property Instance As Configuration
        Get
            Return CachedInstance
        End Get
    End Property

    <ConfigurationProperty(DefaultCachePathKey, IsRequired:=True)>
    Public ReadOnly Property DefaultCachePath As String
        Get
            Return CType(Me(DefaultCachePathKey), String)
        End Get
    End Property

    <ConfigurationProperty(MaxCachedConnectionCountKey, IsRequired:=False, DefaultValue:=10S)>
    Public ReadOnly Property MaxCachedConnectionCount As Short
        Get
            Return CType(Me(MaxCachedConnectionCountKey), Short)
        End Get
    End Property

    <ConfigurationProperty(MaxCacheSizeInMBKey, IsRequired:=True)>
    Public ReadOnly Property MaxCacheSizeInMB As Integer
        Get
            Return CType(Me(MaxCacheSizeInMBKey), Integer)
        End Get
    End Property

    <ConfigurationProperty(OperationCountBeforeSoftCleanupKey, IsRequired:=False, DefaultValue:=100S)>
    Public ReadOnly Property OperationCountBeforeSoftCleanup As Short
        Get
            Return CType(Me(OperationCountBeforeSoftCleanupKey), Short)
        End Get
    End Property
End Class
