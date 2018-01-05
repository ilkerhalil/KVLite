﻿// File name: Program.cs
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

using NUnitLite;
using Serilog;
using Serilog.Events;
using System;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests
{
    internal static class Program
    {
        public static string MySqlConnectionString { get; set; } = @"Server=127.0.0.1;Port=3306;Database=kvlite;Uid=kvlite;Pwd=kvlite;Pooling=true;CharSet=utf8;AutoEnlist=false;SslMode=none;";

        public static string PostgreSqlConnectionString { get; set; } = @"Server=127.0.0.1;Port=5432;Database=postgres;User Id=kvlite;Password=kvlite;Pooling=true;Protocol=3;";

        public static string SqlServerConnectionString { get; set; } = @"Data Source=(LocalDB)\MSSQLLocalDB;Database=kvlite;Integrated Security=True;MultipleActiveResultSets=True;";

        public static int Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.Console()
                .MinimumLevel.Is(LogEventLevel.Warning)
                .Enrich.WithDemystifiedStackTraces()
                .CreateLogger();

            args = LookForConnectionStrings(args);

#if NETSTD20
            return new AutoRun(System.Reflection.Assembly.GetEntryAssembly()).Execute(args, new NUnit.Common.ColorConsoleWriter(), System.Console.In);
#else
            return new AutoRun().Execute(args);
#endif
        }

        private static string[] LookForConnectionStrings(string[] args)
        {
            var mySqlIndex = Array.IndexOf(args, "--mysql");
            if (mySqlIndex >= 0 && args.Length > mySqlIndex + 1)
            {
                MySqlConnectionString = args[mySqlIndex + 1];
                args[mySqlIndex] = args[mySqlIndex + 1] = null;
            }

            var postgreSqlIndex = Array.IndexOf(args, "--postgresql");
            if (postgreSqlIndex >= 0 && args.Length > postgreSqlIndex + 1)
            {
                PostgreSqlConnectionString = args[postgreSqlIndex + 1];
                args[postgreSqlIndex] = args[postgreSqlIndex + 1] = null;
            }

            var sqlServerIndex = Array.IndexOf(args, "--sqlserver");
            if (sqlServerIndex >= 0 && args.Length > sqlServerIndex + 1)
            {
                SqlServerConnectionString = args[sqlServerIndex + 1];
                args[sqlServerIndex] = args[sqlServerIndex + 1] = null;
            }

            return args.Where(a => a != null).ToArray();
        }
    }
}
