using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PommaLabs.GRAMPA.Testing;
using PommaLabs.KVLite;
using PommaLabs.KVLite.Properties;

namespace UnitTests
{
    [TestFixture]
    internal abstract class TestBase
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            DefaultInstance.Clear(CacheReadMode.ExcludeExpiredItems);
        }

        [TearDown]
        public void TearDown()
        {
            DefaultInstance.Clear(CacheReadMode.ExcludeExpiredItems);
        }

        #endregion

        protected abstract ICache DefaultInstance { get; }

        protected const int SmallItemCount = 10;
        protected const int MediumItemCount = 100;
        protected const int LargeItemCount = 1000;

        private const int MinItem = 10000;

        protected static readonly List<string> StringItems = Enumerable
            .Range(MinItem, LargeItemCount)
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToList();

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes_Concurrent(int itemCount)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < itemCount; ++i) {
                var l = i;
                var task = Task.Factory.StartNew(() => {
                    DefaultInstance.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    DefaultInstance.Contains(StringItems[l]);
                });
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i) {
                var l = i;
                var task = Task.Factory.StartNew(() => {
                    DefaultInstance.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    DefaultInstance.Contains(StringItems[l]);
                });
                tasks.Add(task);
            }
            foreach (var task in tasks) {
                task.Wait();
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes_CheckItems(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(DefaultInstance.Contains(StringItems[i]));
            }
            var items = DefaultInstance.GetAllItems();
            for (var i = 0; i < itemCount; ++i) {
                var s = StringItems[i];
                Assert.True(items.Count(x => x.Key == s && (string) x.Value == s) == 1);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                Assert.IsNull(DefaultInstance.Get(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache_Concurrent(int itemCount)
        {
            var tasks = new List<Task<object>>();
            for (var i = 0; i < itemCount; ++i) {
                var l = i;
                var task = DefaultInstance.GetAsync(StringItems[l]);
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i) {
                Assert.IsNull(tasks[i].Result);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.AddMinutes(10));
            }
            for (var i = 0; i < itemCount; ++i) {
                var item = (string) DefaultInstance.Get(StringItems[i]);
                Assert.IsNotNull(item);
                Assert.AreEqual(StringItems[i], item);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var times = new List<DateTime>();
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddSliding(StringItems[i], StringItems[i], TimeSpan.FromMinutes(10));
                times.Add(DateTime.UtcNow.AddMinutes(10));
            }
            Thread.Sleep(1000);
            for (var i = 0; i < itemCount; ++i) {
                var item = DefaultInstance.GetItem(StringItems[i]);
                Assert.IsNotNull(item);
                Assert.GreaterOrEqual(item.UtcExpiry, times[i]);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItems_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var times = new List<DateTime>();
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddSliding(StringItems[i], StringItems[i], TimeSpan.FromMinutes(10));
                times.Add(DateTime.UtcNow.AddMinutes(10));
            }
            Thread.Sleep(1000);
            var items = DefaultInstance.GetAllItems();
            for (var i = 0; i < itemCount; ++i) {
                var item = items[i];
                Assert.IsNotNull(item);
                Assert.GreaterOrEqual(item.UtcExpiry, times[i]);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache_Outdated(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i) {
                Assert.IsNull(DefaultInstance.Get(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddTimed_ValidTime(int itemCount)
        {
            AddTimed(DefaultInstance, itemCount, DateTime.UtcNow.AddMinutes(10));
            var items = new HashSet<string>(DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddTimed_InvalidTime(int itemCount)
        {
            AddTimed(DefaultInstance, itemCount, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            var items = new HashSet<string>(DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddSliding_ValidTime(int itemCount)
        {
            AddSliding(DefaultInstance, itemCount, TimeSpan.FromHours(1));
            var items = new HashSet<string>(DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddSliding_InvalidTime(int itemCount)
        {
            AddSliding(DefaultInstance, itemCount, TimeSpan.FromSeconds(1));
            Thread.Sleep(2000); // Waits to seconds, to let the value expire...
            var items = new HashSet<string>(DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddStatic(int itemCount)
        {
            AddStatic(DefaultInstance, itemCount);
            var items = new HashSet<string>(DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        protected static void AddStatic(ICache instance, int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddStatic(StringItems[i], StringItems[i]);
            }
        }

        protected static void AddSliding(ICache instance, int itemCount, TimeSpan interval)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddSliding(StringItems[i], StringItems[i], interval);
            }
        }

        protected static void AddTimed(ICache instance, int itemCount, DateTime utcTime)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddTimed(StringItems[i], StringItems[i], utcTime);
            }
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullKey()
        {
            DefaultInstance.AddSliding(null, StringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullPartition()
        {
            DefaultInstance.AddSliding(null, StringItems[0], StringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        public void AddSliding_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = TimeSpan.FromMinutes(10);
            DefaultInstance.AddSliding(p, k, Tuple.Create(v1, v2), i);
            var info = DefaultInstance.GetItem(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            var infoValue = info.Value as Tuple<string, string>;
            Assert.AreEqual(v1, infoValue.Item1);
            Assert.AreEqual(v2, infoValue.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullKey()
        {
            DefaultInstance.AddStatic(null, StringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullPartition()
        {
            DefaultInstance.AddStatic(null, StringItems[0], StringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullValue()
        {
            DefaultInstance.AddStatic(StringItems[0], null);
        }

        [Test]
        public void AddStatic_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            DefaultInstance.AddStatic(p, k, Tuple.Create(v1, v2));
            var info = DefaultInstance.GetItem(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            var infoValue = info.Value as Tuple<string, string>;
            Assert.AreEqual(v1, infoValue.Item1);
            Assert.AreEqual(v2, infoValue.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(TimeSpan.FromDays(Settings.Default.DefaultStaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddTimed_HugeValue()
        {
            var k = StringItems[1];
            var v = new byte[20000];
            DefaultInstance.AddTimed(k, v, DateTime.UtcNow.AddMinutes(10));
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
            DefaultInstance.AddTimed(null, StringItems[1], DateTime.UtcNow);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTimed_NullPartition()
        {
            DefaultInstance.AddTimed(null, StringItems[0], StringItems[1], DateTime.UtcNow);
        }

        [Test]
        public void AddTimed_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = DateTime.Now.AddMinutes(10);
            DefaultInstance.AddTimed(p, k, Tuple.Create(v1, v2), e);
            var info = DefaultInstance.GetItem(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            var infoValue = info.Value as Tuple<string, string>;
            Assert.AreEqual(v1, infoValue.Item1);
            Assert.AreEqual(v2, infoValue.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.Date, info.UtcExpiry.Value.Date);
            Assert.AreEqual(e.Hour, info.UtcExpiry.Value.Hour);
            Assert.AreEqual(e.Minute, info.UtcExpiry.Value.Minute);
            Assert.AreEqual(e.Second, info.UtcExpiry.Value.Second);

            Assert.IsNull(info.Interval);
        }

        [Test]
        public void AddTimed_TwoValues_SameKey()
        {
            DefaultInstance.AddTimed(StringItems[0], StringItems[1], DateTime.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[1], DefaultInstance.Get(StringItems[0]));
            DefaultInstance.AddTimed(StringItems[0], StringItems[2], DateTime.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[2], DefaultInstance.Get(StringItems[0]));
        }

        [Test]
        public void Get_LargeDataTable()
        {
            var dt = new RandomDataTableGenerator("A123", "Test", "Pi", "<3", "Pu").GenerateDataTable(LargeItemCount);
            DefaultInstance.AddStatic(dt.TableName, dt);
            var storedDt = DefaultInstance.Get(dt.TableName) as DataTable;
            Assert.AreEqual(dt.Rows.Count, storedDt.Rows.Count);
            for (var i = 0; i < dt.Rows.Count; ++i) {
                Assert.AreEqual(dt.Rows[i].ItemArray.Length, storedDt.Rows[i].ItemArray.Length);
                for (var j = 0; j < dt.Rows[i].ItemArray.Length; ++j) {
                    Assert.AreEqual(dt.Rows[i].ItemArray[j], storedDt.Rows[i].ItemArray[j]);
                }
            }
        }
    }
}