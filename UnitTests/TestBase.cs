using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using PommaLabs.KVLite;

namespace UnitTests
{
   [TestFixture]
   internal abstract class TestBase
   {
      [SetUp]
      public void SetUp()
      {
         DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
      }

      [TearDown]
      public void TearDown()
      {
         DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
      }

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
         for (var i = 0; i < itemCount; ++i)
         {
            DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
            Assert.True(DefaultInstance.Contains(StringItems[i]));
         }
         for (var i = 0; i < itemCount; ++i)
         {
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
         for (var i = 0; i < itemCount; ++i)
         {
            var l = i;
            var task = Task.Factory.StartNew(() =>
            {
               DefaultInstance.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
               DefaultInstance.Contains(StringItems[l]);
            });
            tasks.Add(task);
         }
         for (var i = 0; i < itemCount; ++i)
         {
            var l = i;
            var task = Task.Factory.StartNew(() =>
            {
               DefaultInstance.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
               DefaultInstance.Contains(StringItems[l]);
            });
            tasks.Add(task);
         }
         foreach (var task in tasks)
         {
            task.Wait();
         }
      }

      [TestCase(SmallItemCount)]
      [TestCase(MediumItemCount)]
      [TestCase(LargeItemCount)]
      public void AddTimed_TwoTimes_CheckItems(int itemCount)
      {
         for (var i = 0; i < itemCount; ++i)
         {
            DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
            Assert.True(DefaultInstance.Contains(StringItems[i]));
         }
         for (var i = 0; i < itemCount; ++i)
         {
            DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
            Assert.True(DefaultInstance.Contains(StringItems[i]));
         }
         var items = DefaultInstance.GetAllItems();
         for (var i = 0; i < itemCount; ++i)
         {
            var s = StringItems[i];
            Assert.True(items.Count(x => x.Key == s && (string) x.Value == s) == 1);
         }
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddSliding_NullKey()
      {
         DefaultInstance.AddSliding(null, StringItems[1], TimeSpan.FromSeconds(10));
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddSliding_NullPartition()
      {
         DefaultInstance.AddSliding(null, StringItems[0], StringItems[1], TimeSpan.FromSeconds(10));
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddStatic_NullKey()
      {
         DefaultInstance.AddStatic(null, StringItems[1]);
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddStatic_NullPartition()
      {
         DefaultInstance.AddStatic(null, StringItems[0], StringItems[1]);
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddTimed_NullKey()
      {
         DefaultInstance.AddTimed(null, StringItems[1], DateTime.UtcNow);
      }

      [Test]
      [ExpectedException(typeof (ArgumentNullException))]
      public void AddTimed_NullPartition()
      {
         DefaultInstance.AddTimed(null, StringItems[0], StringItems[1], DateTime.UtcNow);
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

        #region Private Methods

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

        #endregion
   }
}