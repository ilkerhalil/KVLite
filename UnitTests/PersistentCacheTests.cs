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
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Threading.Tasks;
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

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek<string>(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.PeekItem(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.PeekItem<string>(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_EmptyCache_Concurrent(int itemCount)
        {
            var tasks = new List<Task<object>>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = TaskEx.Run(() => DefaultInstance.Peek(StringItems[l]));
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(tasks[i].Result);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_FullCache_ExpiryNotChanged(int itemCount)
        {
            var expiryDate = DateTime.UtcNow.AddMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], expiryDate);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var value = DefaultInstance.Peek<string>(StringItems[i]);
                Assert.IsNotNull(value);
                Assert.AreEqual(StringItems[i], value);
                var item = DefaultInstance.PeekItem<string>(StringItems[i]);
                Assert.AreEqual(expiryDate.Date, item.UtcExpiry.Value.Date);
                Assert.AreEqual(expiryDate.Hour, item.UtcExpiry.Value.Hour);
                Assert.AreEqual(expiryDate.Minute, item.UtcExpiry.Value.Minute);
                Assert.AreEqual(expiryDate.Second, item.UtcExpiry.Value.Second);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_FullCache_Outdated(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek(StringItems[i]));
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