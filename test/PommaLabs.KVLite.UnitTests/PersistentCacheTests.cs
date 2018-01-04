﻿// File name: PersistentCacheTests.cs
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

using Microsoft.Data.Sqlite;
using Ninject;
using NUnit.Framework;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Resources;
using PommaLabs.KVLite.SQLite;
using System;

namespace PommaLabs.KVLite.UnitTests
{
    internal sealed class PersistentCacheTests : AbstractCacheTests<PersistentCache, PersistentCacheSettings, SQLiteCacheConnectionFactory<PersistentCacheSettings>, SqliteConnection>
    {
        private const string BlankPath = "   ";

        #region Cache creation and disposal

        [Test]
        public void NewCache_BlankPath()
        {
            ICache cache;
            try
            {
#pragma warning disable CC0022 // Should dispose object
                cache = new PersistentCache(new PersistentCacheSettings { CacheFile = BlankPath }, clock: Kernel.Get<IClock>());
#pragma warning restore CC0022 // Should dispose object
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCacheFile));
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            ICache cache;
            try
            {
#pragma warning disable CC0022 // Should dispose object
                cache = new PersistentCache(new PersistentCacheSettings { CacheFile = string.Empty }, clock: Kernel.Get<IClock>());
#pragma warning restore CC0022 // Should dispose object
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCacheFile));
            }
        }

        [Test]
        public void NewCache_NullPath()
        {
            ICache cache;
            try
            {
#pragma warning disable CC0022 // Should dispose object
                cache = new PersistentCache(new PersistentCacheSettings { CacheFile = null }, clock: Kernel.Get<IClock>());
#pragma warning restore CC0022 // Should dispose object
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCacheFile));
            }
        }

        [Test]
        public void Dispose_ObjectDisposedExceptionAfterDispose()
        {
            Cache = new PersistentCache(new PersistentCacheSettings());
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
                Cache.AddTimed(t, t, Cache.Clock.UtcNow + TimeSpan.FromMinutes(10));
            }
            Cache.Clear();
            Assert.AreEqual(0, Cache.Count());

            foreach (var t in StringItems)
            {
                Cache.AddTimed(t, t, Cache.Clock.UtcNow + TimeSpan.FromMinutes(10));
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
