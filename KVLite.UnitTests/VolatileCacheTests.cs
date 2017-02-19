// File name: VolatileCacheTests.cs
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

using Ninject;
using NUnit.Framework;
using PommaLabs.CodeServices.Caching;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.SQLite;
using System;
using System.Data.SQLite;

namespace PommaLabs.KVLite.UnitTests
{
    internal sealed class VolatileCacheTests : AbstractCacheTests<VolatileCacheSettings, SQLiteConnection>
    {
        #region Setup/Teardown

        [SetUp]
        public override void SetUp()
        {
            Cache = Kernel.Get<VolatileCache>();
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        #endregion Setup/Teardown

        #region Cache creation and disposal

        [TestCase("")]
        [TestCase((string) null)]
        [TestCase("   ")]
        public void NewCache_BlankName(string name)
        {
            ICache cache;
            try
            {
#pragma warning disable CC0022 // Should dispose object
                cache = new VolatileCache(new VolatileCacheSettings { CacheName = name }, clock: Kernel.Get<IClock>());
#pragma warning restore CC0022 // Should dispose object
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCacheName));
            }
        }

        [TestCase("$$$")]
        [TestCase("a£")]
        [TestCase("1,2")]
        [TestCase("1+aaa")]
        [TestCase("_3?")]
        public void NewCache_WrongName(string name)
        {
            ICache cache;
            try
            {
#pragma warning disable CC0022 // Should dispose object
                cache = new VolatileCache(new VolatileCacheSettings { CacheName = name }, clock: Kernel.Get<IClock>());
#pragma warning restore CC0022 // Should dispose object
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.InvalidCacheName));
            }
        }

        [TestCase("a")]
        [TestCase("a1")]
        [TestCase("a_")]
        [TestCase("a.")]
        [TestCase("_")]
        [TestCase(".")]
        [TestCase("1")]
        [TestCase("1_a")]
        [TestCase("a.b")]
        [TestCase("a...b")]
        public void NewCache_GoodName(string name)
        {
            ICache cache = new VolatileCache(new VolatileCacheSettings { CacheName = name }, clock: Kernel.Get<IClock>());
            Assert.That(cache, Is.Not.Null);
            cache.Dispose();
            Assert.That(cache.Disposed, Is.True);
        }

        [Test]
        public void Dispose_ObjectDisposedExceptionAfterDispose()
        {
            Cache = new VolatileCache(new VolatileCacheSettings());
            Cache.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { Cache.Count(); });
        }

        #endregion Cache creation and disposal

        #region SQLite-specific Clean

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in StringItems)
            {
                Cache.AddTimedToDefaultPartition(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Cache.Clear();
            Assert.AreEqual(0, Cache.Count());
            foreach (var t in StringItems)
            {
                Cache.AddTimedToDefaultPartition(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            var volatileCache = (VolatileCache) Cache;
            volatileCache.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(0, Cache.Count());
            foreach (var t in StringItems)
            {
                Cache.AddTimedToDefaultPartition(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            volatileCache.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, Cache.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in StringItems)
            {
                Cache.AddTimedToDefaultPartition(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            Cache.Clear();
            Assert.AreEqual(0, Cache.Count());

            foreach (var t in StringItems)
            {
                Cache.AddTimedToDefaultPartition(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            var volatileCache = (VolatileCache) Cache;
            volatileCache.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(StringItems.Count, Cache.Count());

            volatileCache.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, Cache.Count());
        }

        #endregion SQLite-specific Clean

        #region Multiple Caches

        [Test, Repeat(50)]
        public void AddStatic_TwoCaches_NoMix()
        {
            const string key = "key";
            using (var another = new VolatileCache(new VolatileCacheSettings { CacheName = "another" }, clock: Kernel.Get<IClock>()))
            {
                try
                {
                    Cache.AddStaticToDefaultPartition(key, 1);
                    another.AddStaticToDefaultPartition(key, 2);
                    Assert.True(Cache.DefaultPartitionContains(key));
                    Assert.True(another.DefaultPartitionContains(key));
                    Assert.AreEqual(1, ((VolatileCache) Cache)[Cache.Settings.DefaultPartition, key].Value);
                    Assert.AreEqual(2, another[Cache.Settings.DefaultPartition, key].Value);

                    another.AddStaticToDefaultPartition(key + key, 3);
                    Assert.False(Cache.DefaultPartitionContains(key + key));
                    Assert.True(another.DefaultPartitionContains(key + key));
                    Assert.AreEqual(3, another[Cache.Settings.DefaultPartition, key + key].Value);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"{ex.Message} - {ex.GetType().Name} - {ex.StackTrace}");
                    Console.Error.WriteLine(Cache.LastError?.Message ?? "First cache has no errors");
                    Console.Error.WriteLine(another.LastError?.Message ?? "Second cache has no errors");
                    throw;
                }
            }
        }

        #endregion Multiple Caches
    }
}
