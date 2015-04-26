// File name: PersistentCacheTests.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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

using System;
using Finsa.CodeServices.Clock;
using Ninject;
using NUnit.Framework;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Core;

namespace UnitTests
{
    internal sealed class PersistentCacheTests : TestBase<PersistentCacheSettings>
    {
        private const string BlankPath = "   ";

        #region Setup/Teardown

        [SetUp]
        public override void SetUp()
        {
            Cache = Kernel.Get<PersistentCache>();
            base.SetUp();
        }

        [TearDown]
        public override void TearDown()
        {
            base.TearDown();
        }

        #endregion Setup/Teardown

        #region Cache Creation

        [Test]
        public void NewCache_BlankPath()
        {
            try
            {
                new PersistentCache(new PersistentCacheSettings { CacheFile = BlankPath }, Kernel.Get<IClock>());
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            try
            {
                new PersistentCache(new PersistentCacheSettings { CacheFile = String.Empty }, Kernel.Get<IClock>());
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_NullPath()
        {
            try
            {
                new PersistentCache(new PersistentCacheSettings { CacheFile = null }, Kernel.Get<IClock>());
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        #endregion Cache Creation

        #region SQLite-specific Clean

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Cache.Clear();
            Assert.AreEqual(0, Cache.Count());
            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            var persistentCache = (PersistentCache) Cache;
            persistentCache.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(0, Cache.Count());
            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            persistentCache.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, Cache.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            Cache.Clear();
            Assert.AreEqual(0, Cache.Count());

            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            var persistentCache = (PersistentCache) Cache;
            persistentCache.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(StringItems.Count, Cache.Count());

            persistentCache.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, Cache.Count());
        }

        #endregion SQLite-specific Clean
    }
}
