// File name: AbstractCacheControllerTests.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using NUnit.Framework;
using PommaLabs.KVLite.WebApi;
using System;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests.WebApi
{
    [TestFixture]
    internal sealed class AbstractCacheControllerTests
    {
        CacheController _controller;

        [SetUp]
        public void SetUp()
        {
            _controller = new CacheController(VolatileCache.DefaultInstance);
        }

        [TearDown]
        public void TearDown()
        {
            _controller.Cache.Clear();
            _controller = null;
        }

        [TestCase("partition", "key")]
        [TestCase("p", "key")]
        [TestCase("partition", "k")]
        [TestCase("pa", "ke")]
        [TestCase("p", "e")]
        public void GetItems_LookupByPartitionAndKey(string partitionLike, string keyLike)
        {
            _controller.Cache.AddStatic("partition", "key", 123);
            _controller.Cache.AddStatic("abc", "def", 123);
            var items = _controller.GetItems(partitionLike, keyLike).ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("partition", items[0].Partition);
            Assert.AreEqual("key", items[0].Key);
            Assert.IsNull(items[0].Value);
        }

        [TestCase(0, 60, 6)]
        [TestCase(30, 30, 2)]
        [TestCase(10, 10, 0)]
        [TestCase(49, 51, 2)]
        [TestCase(30, 50, 6)]
        public void GetItems_LookupByExpiry(int fromExpiry, int toExpiry, int expectedCount)
        {
            _controller.Cache.AddTimed("partition1", "key", 123, _controller.Cache.Clock.UtcNow.AddMinutes(30));
            _controller.Cache.AddTimed("partition2", "key", 123, _controller.Cache.Clock.UtcNow.AddMinutes(40));
            _controller.Cache.AddTimed("partition3", "key", 123, _controller.Cache.Clock.UtcNow.AddMinutes(50));
            _controller.Cache.AddSliding("abc1", "def", 123, TimeSpan.FromMinutes(30));
            _controller.Cache.AddSliding("abc2", "def", 123, TimeSpan.FromMinutes(40));
            _controller.Cache.AddSliding("abc3", "def", 123, TimeSpan.FromMinutes(50));
            var items = _controller.GetItems(fromExpiry: _controller.Cache.Clock.UtcNow.AddMinutes(fromExpiry), toExpiry: _controller.Cache.Clock.UtcNow.AddMinutes(toExpiry)).ToList();
            Assert.AreEqual(expectedCount, items.Count);
        }

        [TestCase("key")]
        [TestCase("ke")]
        [TestCase("k")]
        public void GetPartitionItems_LookupByKey(string keyLike)
        {
            _controller.Cache.AddStatic("partition", "key", 123);
            _controller.Cache.AddStatic("abc", "def", 123);
            var items = _controller.GetPartitionItems("partition", keyLike).ToList();
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("partition", items[0].Partition);
            Assert.AreEqual("key", items[0].Key);
            Assert.IsNull(items[0].Value);
        }

        sealed class CacheController : AbstractCacheController
        {
            public CacheController(ICache cache) : base(cache)
            {
            }
        }
    }
}
