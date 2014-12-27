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
using System.Runtime.Serialization;
using NUnit.Framework;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Properties;

namespace UnitTests
{
    internal sealed class PersistentCacheTests : TestBase
    {
        private const string BlankPath = "   ";
        
        #region Setup/Teardown

        [SetUp]
        public override void SetUp()
        {
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.IgnoreExpiryDate);
        }

        [TearDown]
        public override void TearDown()
        {
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.IgnoreExpiryDate);
        }

        #endregion

        protected override ICache DefaultInstance
        {
           get { return PersistentCache.DefaultInstance; }
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_NotSerializableValue()
        {
            DefaultInstance.AddStatic(StringItems[0], new NotSerializableClass());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_DataContractValue()
        {
            DefaultInstance.AddStatic(StringItems[0], new DataContractClass());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues()
        {
            for (var i = 0; i < Settings.Default.DefaultInsertionCountBeforeCleanup_ForPersistentCache; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues()
        {
            for (var i = 0; i < Settings.Default.DefaultInsertionCountBeforeCleanup_ForPersistentCache; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.AddMinutes(10));
            }
            Assert.AreEqual(Settings.Default.DefaultInsertionCountBeforeCleanup_ForPersistentCache, DefaultInstance.Count());
        }

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in StringItems) {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in StringItems) 
            {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.AddMinutes(10));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(0, DefaultInstance.Count());

            foreach (var t in StringItems)
            {
                DefaultInstance.AddTimed(t, t, DateTime.UtcNow.AddMinutes(10));
            }
            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(StringItems.Count, DefaultInstance.Count());

            PersistentCache.DefaultInstance.Clear(PersistentCacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void NewCache_BlankPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = BlankPath});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = String.Empty});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_NullPath()
        {
            try {
                new PersistentCache(new PersistentCacheSettings {CacheFile = null});
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }
    }

    internal sealed class NotSerializableClass
    {
        public string Pino = "Gino";
    }

    [DataContract]
    internal sealed class DataContractClass
    {
        [DataMember]
        public string Pino = "Gino";
    }
}