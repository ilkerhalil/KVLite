// File name: ConnectionStrings.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT
// NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests
{
    public static class ConnectionStrings
    {
        public static string MySql { get; set; } = @"Server=127.0.0.1;Port=3306;Database=kvlite;Uid=kvlite;Pwd=kvlite;CharSet=utf8;Pooling=true;MinimumPoolSize=1;AutoEnlist=false;";

        public static string Oracle { get; set; } = @"TODO";

        public static string PostgreSql { get; set; } = @"Server=127.0.0.1;Port=5432;Database=postgres;UserId=kvlite;Password=kvlite;MaxAutoPrepare=10;Pooling=true;MinPoolSize=1;Enlist=false;";

        public static string SqlServer { get; set; } = @"Data Source=(LocalDB)\\MSSQLLocalDB;Database=kvlite;IntegratedSecurity=true;MultipleActiveResultSets=true;Pooling=true;Min Pool Size=1;Enlist=false;";

        public static string[] ReadFromCommandLineArguments(string[] args)
        {
            var mySqlIndex = Array.IndexOf(args, "--mysql");
            if (mySqlIndex >= 0 && args.Length > mySqlIndex + 1)
            {
                MySql = args[mySqlIndex + 1];
                args[mySqlIndex] = args[mySqlIndex + 1] = null;
            }

            var postgreSqlIndex = Array.IndexOf(args, "--postgresql");
            if (postgreSqlIndex >= 0 && args.Length > postgreSqlIndex + 1)
            {
                PostgreSql = args[postgreSqlIndex + 1];
                args[postgreSqlIndex] = args[postgreSqlIndex + 1] = null;
            }

            var sqlServerIndex = Array.IndexOf(args, "--sqlserver");
            if (sqlServerIndex >= 0 && args.Length > sqlServerIndex + 1)
            {
                SqlServer = args[sqlServerIndex + 1];
                args[sqlServerIndex] = args[sqlServerIndex + 1] = null;
            }

            return args.Where(a => a != null).ToArray();
        }
    }
}
