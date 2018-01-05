// File name: NinjectConfig.cs
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

using Ninject.Modules;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.SqlServer;

namespace PommaLabs.KVLite.UnitTests
{
    /// <summary>
    ///   Bindings for KVLite.
    /// </summary>
    internal sealed class NinjectConfig : NinjectModule
    {
        public override void Load()
        {
            Bind<ICompressor>()
                .ToConstant(DeflateCompressor.Instance)
                .InSingletonScope();

            Bind<ISerializer>()
                .ToConstant(JsonSerializer.Instance)
                .InSingletonScope();

            Bind<IClock>()
                .ToConstant(new FakeClock(SystemClock.Instance.UtcNow))
                .InTransientScope();

            Bind<IRandom>()
                .To<SystemRandom>()
                .InTransientScope();

            Bind<MySqlCacheSettings>()
                .ToConstant(new MySqlCacheSettings { ConnectionString = Program.MySqlConnectionString })
                .InSingletonScope();

            Bind<MySqlCache>()
                .ToSelf()
                .InSingletonScope();

            Bind<SqlServerCacheSettings>()
                .ToConstant(new SqlServerCacheSettings { ConnectionString = Program.SqlServerConnectionString })
                .InSingletonScope();

            Bind<SqlServerCache>()
                .ToSelf()
                .InSingletonScope();
        }
    }
}
