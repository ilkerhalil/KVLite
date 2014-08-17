//
// CacheContext.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.SQLite;
using Dapper;

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    internal sealed class CacheContext : IDisposable
    {
        private static readonly ConcurrentDictionary<string, ConcurrentStack<IDbConnection>> ConnectionPool =
            new ConcurrentDictionary<string, ConcurrentStack<IDbConnection>>();

        private readonly IDbConnection _connection;
        private bool _disposed;

        #region Construction

        private CacheContext(IDbConnection connection)
        {
            _connection = connection;
        }

        public static CacheContext Create(string connectionString)
        {
            return new CacheContext(GetOrCreateConnection(connectionString));
        }

        #endregion

        #region DB Interaction

        public IDbConnection Connection
        {
            get { return _connection; }
        }

        public IDbTransaction BeginTransaction()
        {
            return _connection.BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public bool Exists(string query, object args, IDbTransaction transaction)
        {
            return _connection.ExecuteScalar<long>(query, args, transaction) > 0;
        }

        public bool Exists(string query, IDbTransaction transaction)
        {
            return Exists(query, null, transaction);
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            if (_disposed) {
                return;
            }
            if (!TryCacheConnection(_connection)) {
                _connection.Dispose();
            }
            GC.SuppressFinalize(this);
            _disposed = true;
        }

        #endregion

        #region Connection Retrieval

        private static IDbConnection GetOrCreateConnection(string connStr)
        {
            return ConnectionPool.ContainsKey(connStr) ? GetCachedConnection(connStr) : CreateNewConnection(connStr);
        }

        private static IDbConnection CreateNewConnection(string connectionString)
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            // Sets PRAGMAs for this new connection.
            var journalSizeLimitInBytes = Configuration.Instance.MaxLogSizeInMB*1024*1024;
            var pragmas = String.Format(Queries.SetPragmas, journalSizeLimitInBytes);
            connection.Execute(pragmas);
            return connection;
        }

        private static IDbConnection GetCachedConnection(string connectionString)
        {
            ConcurrentStack<IDbConnection> connectionList;
            ConnectionPool.TryGetValue(connectionString, out connectionList);
            if (connectionList == null || connectionList.Count == 0) {
                return CreateNewConnection(connectionString);
            }
            IDbConnection connection;
            connectionList.TryPop(out connection);
            return connection ?? CreateNewConnection(connectionString);
        }

        #endregion

        #region Connection Caching

        private static bool TryCacheConnection(IDbConnection connection)
        {
            return ConnectionPool.ContainsKey(connection.ConnectionString)
                ? TryStoreConnection(connection)
                : AddFirstList(connection);
        }

        private static bool TryStoreConnection(IDbConnection connection)
        {
            ConcurrentStack<IDbConnection> connectionList;
            ConnectionPool.TryGetValue(connection.ConnectionString, out connectionList);
            dynamic maxConnCount = Configuration.Instance.MaxCachedConnectionCount;
            if (connectionList == null) {
                return AddFirstList(connection);
            }
            if (connectionList.Count <= maxConnCount) {
                connectionList.Push(connection);
                return true;
            }
            return false;
        }

        private static bool AddFirstList(IDbConnection connection)
        {
            var connectionList = new ConcurrentStack<IDbConnection>();
            connectionList.Push(connection);
            return ConnectionPool.TryAdd(connection.ConnectionString, connectionList);
        }

        #endregion
    }
}