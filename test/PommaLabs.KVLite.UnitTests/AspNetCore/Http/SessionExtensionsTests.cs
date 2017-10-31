// File name: SessionExtensionsTests.cs
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

#if !NET45

using NUnit.Framework;
using PommaLabs.KVLite.AspNetCore.Http;
using PommaLabs.KVLite.Extensibility;
using Shouldly;
using System;

namespace PommaLabs.KVLite.UnitTests.AspNetCore.Http
{
    internal sealed class SessionExtensionsTests : AbstractTests
    {
        #region Setup/Teardown

        private MemorySession _memorySession;

        [SetUp]
        public void SetUp()
        {
            _memorySession = new MemorySession();
        }

        [TearDown]
        public void TearDown()
        {
            _memorySession?.Clear();
            _memorySession = null;
        }

        #endregion Setup/Teardown

        [Test]
        public void ShouldGetTheSameObjectAfterSetWithDefaultSerializer()
        {
            var x = Math.Round(Math.Pow(Math.PI, 2.0), 7);
            var y = new string('a', 21);
            var z = DateTime.Now;
            var t = (x, y, z);

            _memorySession.SetObject(nameof(t), t);
            var fromSession = _memorySession.GetObject<(double x, string y, DateTime z)>(nameof(t));

            fromSession.HasValue.ShouldBeTrue();
            fromSession.Value.x.ShouldBe(x);
            fromSession.Value.y.ShouldBe(y);
            fromSession.Value.z.ShouldBe(z);
        }

        [Test]
        public void ShouldGetTheSameObjectAfterSetWithCustomSerializer()
        {
            var x = Math.Round(Math.Pow(Math.PI, 2.0), 7);
            var y = new string('a', 21);
            var z = DateTime.Now.Date;
            var t = (x, y, z);

            _memorySession.SetObject(BsonSerializer.Instance, nameof(t), t);
            var fromSession = _memorySession.GetObject<(double x, string y, DateTime z)>(BsonSerializer.Instance, nameof(t));

            fromSession.HasValue.ShouldBeTrue();
            fromSession.Value.x.ShouldBe(x);
            fromSession.Value.y.ShouldBe(y);
            fromSession.Value.z.ShouldBe(z);
        }
    }
}

#endif
