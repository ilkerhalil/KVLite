// File name: TestBase.cs
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
using System.Data;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Finsa.CodeServices.Clock;
using Ninject;
using NUnit.Framework;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Properties;
using PommaLabs.KVLite.Utilities.Extensions;
using PommaLabs.KVLite.Utilities.Testing;

namespace UnitTests
{
    [TestFixture]
    internal abstract class TestBase<TCache, TCacheSettings>
        where TCache : CacheBase<TCache, TCacheSettings>
        where TCacheSettings : CacheSettingsBase, new()
    {
        
        protected ICache Cache;

        #region Setup/Teardown

        [SetUp]
        public virtual void SetUp()
        {
            DefaultInstance.Clear();
            Cache.Clear();
        }

        [TearDown]
        public virtual void TearDown()
        {
            DefaultInstance.Clear();
            Cache.Clear();
            Cache = null;
        }

        #endregion Setup/Teardown

        #region Constants

        private const int LargeItemCount = 1000;

        private const int MediumItemCount = 100;

        private const int SmallItemCount = 10;

        private const int MinItem = 10000;

        private readonly List<string> _stringItems = Enumerable
            .Range(MinItem, LargeItemCount)
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToList();
        
        protected readonly IKernel Kernel = new StandardKernel(new NinjectConfig());

        #endregion Constants

        protected abstract TCache DefaultInstance { get; }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullKey()
        {
            Cache.AddSliding(null, _stringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullPartition()
        {
            Cache.AddSliding(null, _stringItems[0], _stringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        public void AddSliding_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            var i = TimeSpan.FromMinutes(10);
            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i);
            var info = Cache.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void AddSlidingAsync_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            var i = TimeSpan.FromMinutes(10);
            DefaultInstance.AddSlidingAsync(p, k, Tuple.Create(v1, v2), i).Wait();
            var info = DefaultInstance.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullKey()
        {
            DefaultInstance.AddStatic(null, _stringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullPartition()
        {
            DefaultInstance.AddStatic(null, _stringItems[0], _stringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullValue()
        {
            DefaultInstance.AddStatic(_stringItems[0], null);
        }

        [Test]
        public void AddStatic_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            DefaultInstance.AddStatic(p, k, Tuple.Create(v1, v2));
            var info = DefaultInstance.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(TimeSpan.FromDays(Settings.Default.AllCaches_DefaultStaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddStaticAsync_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            DefaultInstance.AddStaticAsync(p, k, Tuple.Create(v1, v2)).Wait();
            var info = DefaultInstance.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(TimeSpan.FromDays(Settings.Default.AllCaches_DefaultStaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddTimed_HugeValue()
        {
            var k = _stringItems[1];
            var v = new byte[20000];
            DefaultInstance.AddTimed(k, v, Cache.Clock.UtcNow.AddMinutes(10));
            var info = DefaultInstance.GetItem(k);
            Assert.IsNotNull(info);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v, info.Value);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.IsNull(info.Interval);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTimed_NullKey()
        {
            DefaultInstance.AddTimed(null, _stringItems[1], Cache.Clock.UtcNow);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTimed_NullPartition()
        {
            DefaultInstance.AddTimed(null, _stringItems[0], _stringItems[1], Cache.Clock.UtcNow);
        }

        [Test]
        public void AddTimed_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            var e = Cache.Clock.UtcNow.AddMinutes(10);
            DefaultInstance.AddTimed(p, k, Tuple.Create(v1, v2), e);
            var info = DefaultInstance.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.Date, info.UtcExpiry.Value.Date);
            Assert.AreEqual(e.Hour, info.UtcExpiry.Value.Hour);
            Assert.AreEqual(e.Minute, info.UtcExpiry.Value.Minute);
            Assert.AreEqual(e.Second, info.UtcExpiry.Value.Second);

            Assert.IsNull(info.Interval);
        }

        [Test]
        public void AddTimedAsync_RightInfo()
        {
            var p = _stringItems[0];
            var k = _stringItems[1];
            var v1 = _stringItems[2];
            var v2 = _stringItems[3];
            var e = Cache.Clock.UtcNow.AddMinutes(10);
            DefaultInstance.AddTimedAsync(p, k, Tuple.Create(v1, v2), e).Wait();
            var info = DefaultInstance.GetItem<Tuple<string, string>>(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.Date, info.UtcExpiry.Value.Date);
            Assert.AreEqual(e.Hour, info.UtcExpiry.Value.Hour);
            Assert.AreEqual(e.Minute, info.UtcExpiry.Value.Minute);
            Assert.AreEqual(e.Second, info.UtcExpiry.Value.Second);

            Assert.IsNull(info.Interval);
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(_stringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes_CheckItems(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(_stringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(_stringItems[i]));
            }
            var items = DefaultInstance.GetManyItems();
            for (var i = 0; i < itemCount; ++i)
            {
                var s = _stringItems[i];
                Assert.True(items.Count(x => x.Key == s && (string) x.Value == s) == 1);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes_Concurrent(int itemCount)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Factory.StartNew(() =>
                {
                    DefaultInstance.AddTimed(_stringItems[l], _stringItems[l], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    DefaultInstance.Contains(_stringItems[l]);
                });
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Factory.StartNew(() =>
                {
                    DefaultInstance.AddTimed(_stringItems[l], _stringItems[l], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    DefaultInstance.Contains(_stringItems[l]);
                });
                tasks.Add(task);
            }
            foreach (var task in tasks)
            {
                task.Wait();
            }
        }

        [Test]
        public void AddTimed_TwoValues_SameKey()
        {
            DefaultInstance.AddTimed(_stringItems[0], _stringItems[1], Cache.Clock.UtcNow.AddMinutes(10));
            Assert.AreEqual(_stringItems[1], DefaultInstance.Get(_stringItems[0]));
            DefaultInstance.AddTimed(_stringItems[0], _stringItems[2], Cache.Clock.UtcNow.AddMinutes(10));
            Assert.AreEqual(_stringItems[2], DefaultInstance.Get(_stringItems[0]));
        }

        [Test]
        public void Count_EmptyCache()
        {
            Assert.AreEqual(0, DefaultInstance.Count());
            Assert.AreEqual(0, DefaultInstance.Count(_stringItems[0]));
            Assert.AreEqual(0, DefaultInstance.LongCount());
            Assert.AreEqual(0, DefaultInstance.LongCount(_stringItems[0]));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Count_FullCache_OnePartition(int itemCount)
        {
            var partition = _stringItems[0];
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddStatic(partition, _stringItems[i], _stringItems[i]);
            }
            Assert.AreEqual(itemCount, DefaultInstance.Count());
            Assert.AreEqual(itemCount, DefaultInstance.Count(partition));
            Assert.AreEqual(itemCount, DefaultInstance.LongCount());
            Assert.AreEqual(itemCount, DefaultInstance.LongCount(partition));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Count_FullCache_TwoPartitions(int itemCount)
        {
            var i = 0;
            var partition1 = _stringItems[0];
            var p1Count = itemCount / 3;
            for (i = 0; i < itemCount / 3; ++i)
            {
                DefaultInstance.AddStatic(partition1, _stringItems[i], _stringItems[i]);
            }
            var partition2 = _stringItems[1];
            for (; i < itemCount; ++i)
            {
                DefaultInstance.AddStatic(partition2, _stringItems[i], _stringItems[i]);
            }
            Assert.AreEqual(itemCount, DefaultInstance.Count());
            Assert.AreEqual(p1Count, DefaultInstance.Count(partition1));
            Assert.AreEqual(itemCount - p1Count, DefaultInstance.Count(partition2));
            Assert.AreEqual(itemCount, DefaultInstance.LongCount());
            Assert.AreEqual(p1Count, DefaultInstance.LongCount(partition1));
            Assert.AreEqual(itemCount - p1Count, DefaultInstance.LongCount(partition2));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Get(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Get<string>(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.GetItem(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.GetItem<string>(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache_Concurrent(int itemCount)
        {
            var tasks = new List<Task<object>>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Run(() => DefaultInstance.Get(_stringItems[l]));
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
        public void Get_FullCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var item = DefaultInstance.Get<string>(_stringItems[i]);
                Assert.IsNotNull(item);
                Assert.AreEqual(_stringItems[i], item);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache_Outdated(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Get(_stringItems[i]));
            }
        }

        [Test]
        public void Get_LargeDataTable()
        {
            var dt = new RandomDataTableGenerator("A123", "Test", "Pi", "<3", "Pu").GenerateDataTable(LargeItemCount);
            DefaultInstance.AddStatic(dt.TableName, dt);
            var storedDt = DefaultInstance.Get(dt.TableName) as DataTable;
            Assert.AreEqual(dt.Rows.Count, storedDt.Rows.Count);
            for (var i = 0; i < dt.Rows.Count; ++i)
            {
                Assert.AreEqual(dt.Rows[i].ItemArray.Length, storedDt.Rows[i].ItemArray.Length);
                for (var j = 0; j < dt.Rows[i].ItemArray.Length; ++j)
                {
                    Assert.AreEqual(dt.Rows[i].ItemArray[j], storedDt.Rows[i].ItemArray[j]);
                }
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddSliding_InvalidTime(int itemCount)
        {
            AddSliding(DefaultInstance, itemCount, TimeSpan.FromSeconds(1));
            Cache.Clock.As<MockClock>().Add(TimeSpan.FromSeconds(2));
            var items = new HashSet<string>(DefaultInstance.GetManyItems().Select(i => i.Value as string));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(_stringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetManyItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddSliding_ValidTime(int itemCount)
        {
            AddSliding(DefaultInstance, itemCount, TimeSpan.FromHours(1));
            var items = new HashSet<string>(DefaultInstance.GetManyItems().Select(i => i.Value as string));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetManyItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddStatic(int itemCount)
        {
            AddStatic(DefaultInstance, itemCount);
            var items = new HashSet<string>(DefaultInstance.GetManyItems().Select(i => i.Value as string));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetManyItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddTimed_InvalidTime(int itemCount)
        {
            AddTimed(DefaultInstance, itemCount, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            var items = new HashSet<string>(DefaultInstance.GetManyItems().Select(i => i.Value as string));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(_stringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetManyItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddTimed_ValidTime(int itemCount)
        {
            AddTimed(DefaultInstance, itemCount, Cache.Clock.UtcNow.AddMinutes(10));
            var items = new HashSet<string>(DefaultInstance.GetManyItems().Select(i => i.Value as string));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetManyItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var interval = TimeSpan.FromMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddSliding(_stringItems[i], _stringItems[i], interval);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var item = DefaultInstance.GetItem(_stringItems[i]);
                Assert.IsNotNull(item);
                Assert.AreEqual(item.UtcExpiry.Value.ToUnixTime(), (Cache.Clock.UtcNow + interval).ToUnixTime());
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetManyItems_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var interval = TimeSpan.FromMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddSliding(_stringItems[i], _stringItems[i], interval);
            }
            Cache.Clock.As<MockClock>().Add(TimeSpan.FromMinutes(1));
            var items = DefaultInstance.GetManyItems();
            for (var i = 0; i < itemCount; ++i)
            {
                var item = items[i];
                Assert.IsNotNull(item);
                Assert.AreEqual(item.UtcExpiry.Value.ToUnixTime(), (Cache.Clock.UtcNow + interval).ToUnixTime());
            }
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues()
        {
            for (var i = 0; i < Settings.Default.PersistentCache_DefaultInsertionCountBeforeAutoClean; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues()
        {
            for (var i = 0; i < Settings.Default.PersistentCache_DefaultInsertionCountBeforeAutoClean; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
            }
            Assert.AreEqual(Settings.Default.PersistentCache_DefaultInsertionCountBeforeAutoClean, DefaultInstance.Count());
        }

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in _stringItems)
            {
                DefaultInstance.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in _stringItems)
            {
                DefaultInstance.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
            foreach (var t in _stringItems)
            {
                DefaultInstance.AddTimed(t, t, Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in _stringItems)
            {
                DefaultInstance.AddTimed(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            DefaultInstance.Clear();
            Assert.AreEqual(0, DefaultInstance.Count());

            foreach (var t in _stringItems)
            {
                DefaultInstance.AddTimed(t, t, Cache.Clock.UtcNow.AddMinutes(10));
            }
            DefaultInstance.Clear(CacheReadMode.ConsiderExpiryDate);
            Assert.AreEqual(_stringItems.Count, DefaultInstance.Count());

            DefaultInstance.Clear(CacheReadMode.IgnoreExpiryDate);
            Assert.AreEqual(0, DefaultInstance.Count());
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek<string>(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.PeekItem(_stringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.PeekItem<string>(_stringItems[i]));
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
                var task = Task.Run(() => DefaultInstance.Peek(_stringItems[l]));
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
            var expiryDate = Cache.Clock.UtcNow.AddMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], expiryDate);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var value = DefaultInstance.Peek<string>(_stringItems[i]);
                Assert.IsNotNull(value);
                Assert.AreEqual(_stringItems[i], value);
                var item = DefaultInstance.PeekItem<string>(_stringItems[i]);
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
                DefaultInstance.AddTimed(_stringItems[i], _stringItems[i], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsNull(DefaultInstance.Peek(_stringItems[i]));
            }
        }

        #region Serialization

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_NotSerializableValue()
        {
            DefaultInstance.AddStatic(_stringItems[0], new NotSerializableClass());
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void AddStatic_DataContractValue()
        {
            DefaultInstance.AddStatic(_stringItems[0], new DataContractClass());
        }

        #endregion Serialization

        #region BCL Collections

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_Dictionary(int itemCount)
        {
            var l = Enumerable.Range(1, itemCount).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture));
            DefaultInstance.AddStatic("dict", l);
            var lc = DefaultInstance.Get<Dictionary<int, string>>("dict");
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_List(int itemCount)
        {
            var l = new List<int>(Enumerable.Range(1, itemCount));
            DefaultInstance.AddStatic("list", l);
            var lc = DefaultInstance.Get<List<int>>("list");
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_HashSet(int itemCount)
        {
            var l = new HashSet<int>(Enumerable.Range(1, itemCount));
            DefaultInstance.AddStatic("set", l);
            var lc = DefaultInstance.Get<HashSet<int>>("set");
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_SortedSet(int itemCount)
        {
            var l = new SortedSet<int>(Enumerable.Range(1, itemCount));
            DefaultInstance.AddStatic("set", l);
            var lc = DefaultInstance.Get<SortedSet<int>>("set");
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_SortedList(int itemCount)
        {
            var l = new SortedList<int, string>(Enumerable.Range(1, itemCount).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture)));
            DefaultInstance.AddStatic("list", l);
            var lc = DefaultInstance.Get<SortedList<int, string>>("list");
            Assert.True(l.SequenceEqual(lc));
        }

        #endregion BCL Collections

        #region Private Methods

        private void AddSliding(ICache instance, int itemCount, TimeSpan interval)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddSliding(_stringItems[i], _stringItems[i], interval);
            }
        }

        private void AddStatic(ICache instance, int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddStatic(_stringItems[i], _stringItems[i]);
            }
        }

        private void AddTimed(ICache instance, int itemCount, DateTime utcTime)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddTimed(_stringItems[i], _stringItems[i], utcTime);
            }
        }

        #endregion Private Methods
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