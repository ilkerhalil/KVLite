using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using PommaLabs.KVLite;

namespace UnitTests
{
    [TestFixture]
    sealed class PersistentCacheTests
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

        private const int SmallItemCount = 10;
        private const int MediumItemCount = 100;
        private const int LargeItemCount = 1000;
        private const int MinItem = 10000;
        private const string BlankPath = "   ";

        private static readonly List<string> StringItems = (
            from x in Enumerable.Range(MinItem, LargeItemCount)
            select x.ToString(CultureInfo.InvariantCulture)).ToList();

        private PersistentCache _fileCache;

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullPartition()
        {
            PersistentCache.DefaultInstance.AddSliding(null, StringItems[0], StringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullPartition()
        {
            PersistentCache.DefaultInstance.AddStatic(null, StringItems[0], StringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTimed_NullPartition()
        {
            PersistentCache.DefaultInstance.AddTimed(null, StringItems[0], StringItems[1], DateTime.UtcNow);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddSliding_NullKey()
        {
            PersistentCache.DefaultInstance.AddSliding(null, StringItems[1], TimeSpan.FromSeconds(10));
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddStatic_NullKey()
        {
            PersistentCache.DefaultInstance.AddStatic(null, StringItems[1]);
        }

        [Test]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddTimed_NullKey()
        {
            PersistentCache.DefaultInstance.AddTimed(null, StringItems[1], DateTime.UtcNow);
        }

        [Test]
        public void Get_LargeDataTable()
        {
            var dt = new RandomDataTableGenerator("A123", "Test", "Pi", "<3", "Pu").GenerateDataTable(LargeItemCount);
            PersistentCache.DefaultInstance.AddStatic(dt.TableName, dt);
            var storedDt = PersistentCache.DefaultInstance.Get(dt.TableName) as DataTable;
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
        public void Add_TwoTimes(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(_fileCache.Contains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i) {
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(_fileCache.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Add_TwoTimes_Concurrent(int itemCount)
        {
            var tasks = new List<Task>();
            for (var i = 0; i < itemCount; ++i) {
                var l = i;
                var task = Task.Factory.StartNew(() => {
                    _fileCache.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    _fileCache.Contains(StringItems[l]);
                });
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i) {
                var l = i;
                var task = Task.Factory.StartNew(() => {
                    _fileCache.AddTimed(StringItems[l], StringItems[l], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    _fileCache.Contains(StringItems[l]);
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
        public void Add_TwoTimes_CheckItems(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(_fileCache.Contains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i) {
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(_fileCache.Contains(StringItems[i]));
            }
            var items = _fileCache.GetAllItems();
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
                Assert.IsNull(_fileCache.Get(StringItems[i]));
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
                var task = _fileCache.GetAsync(StringItems[l]);
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
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.AddMinutes(10));
            }
            for (var i = 0; i < itemCount; ++i) {
                var item = (string) _fileCache.Get(StringItems[i]);
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
                _fileCache.AddTimed(StringItems[i], StringItems[i], DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i) {
                Assert.IsNull(_fileCache.Get(StringItems[i]));
            }
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
        public void GetAll_RightItems_AfterAddTimed_ValidTime(int itemCount)
        {
            AddTimed(PersistentCache.DefaultInstance, itemCount, DateTime.UtcNow.AddMinutes(10));
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
        public void GetAll_RightItems_AfterAddTimed_InvalidTime(int itemCount)
        {
            AddTimed(PersistentCache.DefaultInstance, itemCount, DateTime.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
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

        #region Private Methods

        private static void AddStatic(ICache instance, int itemCount)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddStatic(StringItems[i], StringItems[i]);
            }
        }

        private static void AddSliding(ICache instance, int itemCount, TimeSpan interval)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddSliding(StringItems[i], StringItems[i], interval);
            }
        }

        private static void AddTimed(ICache instance, int itemCount, DateTime utcTime)
        {
            for (var i = 0; i < itemCount; ++i) {
                instance.AddTimed(StringItems[i], StringItems[i], utcTime);
            }
        }

        #endregion
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