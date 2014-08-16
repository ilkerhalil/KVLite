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

namespace KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    internal sealed class CacheContext : IDisposable
    {
        private static readonly ConcurrentDictionary<string, ConcurrentStack<SQLiteConnection>> ConnectionPool =
            new ConcurrentDictionary<string, ConcurrentStack<SQLiteConnection>>();

        private readonly SQLiteConnection _connection;
        private bool _disposed;

        #region Construction

        private CacheContext(SQLiteConnection connection)
        {
            _connection = connection;
        }

        public static CacheContext Create(string connectionString)
        {
            dynamic connection = GetOrCreateConnection(connectionString);
            return new CacheContext(connection);
        }

        #endregion

        #region DB Interaction

        public SQLiteConnection Connection
        {
            get { return _connection; }
        }

        public void ExecuteNonQuery(string query, SQLiteTransaction transaction)
        {
            using (var cmd = new SQLiteCommand(query, Connection, transaction)) {
                cmd.ExecuteNonQuery();
            }
        }

        //public bool Exists(string query, object args)
        //{
        //    return _connection.ExecuteScalar<int>(query, args) > 0;
        //}

        public bool Exists(string query, SQLiteTransaction transaction)
        {
            using (var cmd = new SQLiteCommand(query, Connection, transaction)) {
                return ((long) cmd.ExecuteScalar(CommandBehavior.SingleResult)) > 0;
            }
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

        private static SQLiteConnection GetOrCreateConnection(string connenctionString)
        {
            if (ConnectionPool.ContainsKey(connenctionString)) {
                return GetCachedConnection(connenctionString);
            }
            return CreateNewConnection(connenctionString);
        }

        private static SQLiteConnection CreateNewConnection(string connectionString)
        {
            var connection = new SQLiteConnection(connectionString);
            connection.Open();
            return connection;
        }

        private static SQLiteConnection GetCachedConnection(string connectionString)
        {
            ConcurrentStack<SQLiteConnection> connectionList;
            ConnectionPool.TryGetValue(connectionString, out connectionList);
            if (connectionList == null || connectionList.Count == 0) {
                return CreateNewConnection(connectionString);
            }
            SQLiteConnection connection;
            connectionList.TryPop(out connection);
            return connection ?? CreateNewConnection(connectionString);
        }

        #endregion

        #region Connection Caching

        private static bool TryCacheConnection(SQLiteConnection connection)
        {
            return ConnectionPool.ContainsKey(connection.ConnectionString)
                ? TryStoreConnection(connection)
                : AddFirstList(connection);
        }

        private static bool TryStoreConnection(SQLiteConnection connection)
        {
            ConcurrentStack<SQLiteConnection> connectionList;
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

        private static bool AddFirstList(SQLiteConnection connection)
        {
            var connectionList = new ConcurrentStack<SQLiteConnection>();
            connectionList.Push(connection);
            return ConnectionPool.TryAdd(connection.ConnectionString, connectionList);
        }

        #endregion
    }
}