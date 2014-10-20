using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using PommaLabs.KVLite;

namespace UnitTests
{
    internal sealed class PersistentCacheTests : TestBase
    {
        #region Setup/Teardown

        [SetUp]
        public void SetUp()
        {
            _fileCache = new PersistentCache();
            _fileCache.Clear(CacheReadMode.IgnoreExpirationDate);
        }

        [TearDown]
        public void TearDown()
        {
            _fileCache.Clear(CacheReadMode.IgnoreExpirationDate);
            _fileCache = null;
        }

        #endregion

        private const string BlankPath = "   ";

        private PersistentCache _fileCache;

        protected override ICache DefaultInstance
        {
           get { return PersistentCache.DefaultInstance; }
        }

        [Test]
        public void AddSliding_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = TimeSpan.FromMinutes(10);
            _fileCache.AddSliding(p, k, Tuple.Create(v1, v2), i);
            var info = _fileCache.GetItem(p, k);
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
        public void AddStatic_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            _fileCache.AddStatic(p, k, Tuple.Create(v1, v2));
            var info = _fileCache.GetItem(p, k);
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            var infoValue = info.Value as Tuple<string, string>;
            Assert.AreEqual(v1, infoValue.Item1);
            Assert.AreEqual(v2, infoValue.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(TimeSpan.FromDays(Configuration.Instance.DefaultStaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddTimed_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = DateTime.Now.AddMinutes(10);
            _fileCache.AddTimed(p, k, Tuple.Create(v1, v2), e);
            var info = _fileCache.GetItem(p, k);
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
        [ExpectedException(typeof(ArgumentNullException))]
        public void Add_NullValue()
        {
            PersistentCache.DefaultInstance.AddStatic(StringItems[0], null);
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void Add_NotSerializableValue()
        {
            PersistentCache.DefaultInstance.AddStatic(StringItems[0], new NotSerializableClass());
        }

        [Test]
        public void Add_HugeValue()
        {
            var k = StringItems[1];
            var v = new byte[20000];
            _fileCache.AddTimed(k, v, DateTime.UtcNow.AddMinutes(10));
            var info = _fileCache.GetItem(k);
            Assert.IsNotNull(info);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v, info.Value);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.IsNull(info.Interval);
        }

        [Test]
        public void Add_TwoValues_SameKey()
        {
            PersistentCache.DefaultInstance.AddTimed(StringItems[0], StringItems[1], DateTime.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[1], PersistentCache.DefaultInstance.Get(StringItems[0]));
            PersistentCache.DefaultInstance.AddTimed(StringItems[0], StringItems[2], DateTime.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[2], PersistentCache.DefaultInstance.Get(StringItems[0]));
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues()
        {
            for (var i = 0; i < Configuration.Instance.OperationCountBeforeSoftCleanup; ++i) {
                PersistentCache.DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            Assert.AreEqual(0, PersistentCache.DefaultInstance.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues()
        {
            for (var i = 0; i < Configuration.Instance.OperationCountBeforeSoftCleanup; ++i) {
                PersistentCache.DefaultInstance.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.AddMinutes(10));
            }
            Assert.AreEqual(Configuration.Instance.OperationCountBeforeSoftCleanup, PersistentCache.DefaultInstance.Count());
        }

        [Test]
        public void Clean_InvalidValues()
        {
            foreach (var t in StringItems) {
                PersistentCache.DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            PersistentCache.DefaultInstance.Clear();
            Assert.AreEqual(0, PersistentCache.DefaultInstance.Count());
            foreach (var t in StringItems) {
                PersistentCache.DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            PersistentCache.DefaultInstance.Clear(CacheReadMode.ConsiderExpirationDate);
            Assert.AreEqual(0, PersistentCache.DefaultInstance.Count());
            foreach (var t in StringItems) {
                PersistentCache.DefaultInstance.AddTimed(t, t, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
            Assert.AreEqual(0, PersistentCache.DefaultInstance.Count());
        }

        [Test]
        public void Clean_ValidValues()
        {
            foreach (var t in StringItems) {
                PersistentCache.DefaultInstance.AddTimed(t, t, DateTime.UtcNow.AddMinutes(10));
            }
            PersistentCache.DefaultInstance.Clear();
            Assert.AreEqual(StringItems.Count, PersistentCache.DefaultInstance.Count());
            PersistentCache.DefaultInstance.Clear(CacheReadMode.ConsiderExpirationDate);
            Assert.AreEqual(StringItems.Count, PersistentCache.DefaultInstance.Count());
            PersistentCache.DefaultInstance.Clear(CacheReadMode.IgnoreExpirationDate);
            Assert.AreEqual(0, PersistentCache.DefaultInstance.Count());
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddSliding_ValidTime(int itemCount)
        {
            AddSliding(PersistentCache.DefaultInstance, itemCount, TimeSpan.FromHours(1));
            var items = new HashSet<string>(PersistentCache.DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddSliding_InvalidTime(int itemCount)
        {
            AddSliding(PersistentCache.DefaultInstance, itemCount, TimeSpan.FromSeconds(1));
            Thread.Sleep(2000); // Waits to seconds, to let the value expire...
            var items = new HashSet<string>(PersistentCache.DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.False(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetAll_RightItems_AfterAddStatic(int itemCount)
        {
            AddStatic(PersistentCache.DefaultInstance, itemCount);
            var items = new HashSet<string>(PersistentCache.DefaultInstance.GetAll().Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllAsync().Result.Cast<string>());
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItems().Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(PersistentCache.DefaultInstance.GetAllItemsAsync().Result.Select(x => (string) x.Value));
            for (var i = 0; i < itemCount; ++i) {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [Test]
        public void NewCache_BlankPath()
        {
            try {
                new PersistentCache(BlankPath);
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_EmptyPath()
        {
            try {
                new PersistentCache(String.Empty);
            } catch (Exception ex) {
                Assert.IsInstanceOf<ArgumentException>(ex);
                Assert.True(ex.Message.Contains(ErrorMessages.NullOrEmptyCachePath));
            }
        }

        [Test]
        public void NewCache_NullPath()
        {
            try {
                new PersistentCache(null);
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

    public sealed class RandomDataTableGenerator
    {
        private readonly string[] _columnNames;
        private readonly Random _random = new Random();

        public RandomDataTableGenerator(params string[] columnNames)
        {
            _columnNames = columnNames.Clone() as string[];
        }

        public DataTable GenerateDataTable(int rowCount)
        {
            var dt = new DataTable("RANDOMLY_GENERATED_DATA_TABLE_" + _random.Next());
            foreach (var columnName in _columnNames) {
                dt.Columns.Add(columnName);
            }
            for (var i = 0; i < rowCount; ++i) {
                var row = new object[_columnNames.Length];
                for (var j = 0; j < row.Length; ++j) {
                    row[j] = _random.Next(100, 999).ToString(CultureInfo.InvariantCulture);
                }
                dt.Rows.Add(row);
            }
            return dt;
        }
    }
}