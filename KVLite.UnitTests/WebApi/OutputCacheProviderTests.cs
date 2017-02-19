// File name: OutputCacheProviderTests.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using NUnit.Framework;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.WebApi;

namespace PommaLabs.KVLite.UnitTests.WebApi
{
    internal sealed class OutputCacheProviderTests : AbstractTests
    {
        private OutputCacheProvider _outputCache;

        [SetUp]
        public void SetUp()
        {
            _outputCache = new OutputCacheProvider(new PersistentCache(new PersistentCacheSettings(), clock: FakeClock.Instance));
        }

        [TearDown]
        public void TearDown()
        {
            _outputCache = null;
        }

        [Test]
        public void Add_One_Valid()
        {
            _outputCache.Add("a", "b", _outputCache.Cache.Clock.UtcNow.AddMinutes(10));
            Assert.AreEqual("b", _outputCache.Get("a"));
            Assert.AreEqual("b", _outputCache.Get<string>("a"));
        }
    }
}
