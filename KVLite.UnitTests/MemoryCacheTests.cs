// File name: PersistentCacheTests.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

using PommaLabs.CodeServices.Caching;
using PommaLabs.CodeServices.Common;
using PommaLabs.CodeServices.Common.Threading.Tasks;
using PommaLabs.CodeServices.Serialization;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using PommaLabs.KVLite.Memory;

namespace PommaLabs.KVLite.UnitTests
{
    [TestFixture]
    sealed class MemoryCacheTests
    {
        const int LargeItemCount = 1000;
        const int MediumItemCount = 100;
        const int SmallItemCount = 10;

        const int MinItem = 10000;

        readonly List<string> StringItems = Enumerable
            .Range(MinItem, LargeItemCount)
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToList();

        #region Setup/Teardown

        MemoryCache Cache;

        [SetUp]
        public void SetUp()
        {
            Cache = new MemoryCache(new MemoryCacheSettings());
            Cache.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Cache?.Clear();
            }
#pragma warning disable RECS0022 // A catch clause that catches System.Exception and has an empty body
#pragma warning disable CC0004 // Catch block cannot be empty
            catch
            {
            }
#pragma warning restore CC0004 // Catch block cannot be empty
#pragma warning restore RECS0022 // A catch clause that catches System.Exception and has an empty body
            finally
            {
                Cache = null;
            }
        }

        #endregion Setup/Teardown

        #region Partition/Key serialization

        [Test]
        public void PartitionKeySerialization_BinarySerializer()
        {
            Cache = new MemoryCache(new MemoryCacheSettings(), serializer: new BinarySerializer());

            Cache.AddStatic(StringItems[0], StringItems[1], StringItems[2]);
            var item = Cache.GetItem<string>(StringItems[0], StringItems[1]);

            Assert.IsNotNull(item);
            Assert.AreEqual(StringItems[0], item.Value.Partition);
            Assert.AreEqual(StringItems[1], item.Value.Key);
            Assert.AreEqual(StringItems[2], item.Value.Value);
        }

        [Test]
        public void PartitionKeySerialization_BsonSerializer()
        {
            Cache = new MemoryCache(new MemoryCacheSettings(), serializer: new BsonSerializer());

            Cache.AddStatic(StringItems[0], StringItems[1], StringItems[2]);
            var item = Cache.GetItem<string>(StringItems[0], StringItems[1]);

            Assert.IsNotNull(item);
            Assert.AreEqual(StringItems[0], item.Value.Partition);
            Assert.AreEqual(StringItems[1], item.Value.Key);
            Assert.AreEqual(StringItems[2], item.Value.Value);
        }

        [Test]
        public void PartitionKeySerialization_JsonSerializer()
        {
            Cache = new MemoryCache(new MemoryCacheSettings(), serializer: new JsonSerializer());

            Cache.AddStatic(StringItems[0], StringItems[1], StringItems[2]);
            var item = Cache.GetItem<string>(StringItems[0], StringItems[1]);

            Assert.IsNotNull(item);
            Assert.AreEqual(StringItems[0], item.Value.Partition);
            Assert.AreEqual(StringItems[1], item.Value.Key);
            Assert.AreEqual(StringItems[2], item.Value.Value);
        }

        [Test]
        public void PartitionKeySerialization_XmlSerializer()
        {
            Cache = new MemoryCache(new MemoryCacheSettings(), serializer: new XmlSerializer());

            Cache.AddStatic(StringItems[0], StringItems[1], StringItems[2]);
            var item = Cache.GetItem<string>(StringItems[0], StringItems[1]);

            Assert.IsNotNull(item);
            Assert.AreEqual(StringItems[0], item.Value.Partition);
            Assert.AreEqual(StringItems[1], item.Value.Key);
            Assert.AreEqual(StringItems[2], item.Value.Value);
        }

        [Test]
        public void PartitionKeySerialization_YamlSerializer()
        {
            Cache = new MemoryCache(new MemoryCacheSettings(), serializer: new YamlSerializer());

            Cache.AddStatic(StringItems[0], StringItems[1], StringItems[2]);
            var item = Cache.GetItem<string>(StringItems[0], StringItems[1]);

            Assert.IsNotNull(item);
            Assert.AreEqual(StringItems[0], item.Value.Partition);
            Assert.AreEqual(StringItems[1], item.Value.Key);
            Assert.AreEqual(StringItems[2], item.Value.Value);
        }

        #endregion Partition/Key serialization

        #region SystemMemoryCache disposal

        [Test]
        public void Dispose_DefaultSystemMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheSettings());
            Cache.Dispose();
        }

        [Test]
        public void Dispose_DefaultSystemMemoryCache_TwoTimes()
        {
            Cache = new MemoryCache(new MemoryCacheSettings());
            Cache.Dispose();
            Cache.Dispose();
        }

        [Test]
        public void Dispose_DefaultSystemMemoryCache_CannotWorkAfterDispose()
        {
            Cache = new MemoryCache(new MemoryCacheSettings());
            Cache.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { Cache.Count(); });        
        }

        [Test]
        public void Dispose_CustomSystemMemoryCache()
        {
            Cache = new MemoryCache(new MemoryCacheSettings { CacheName = "PINO" });
            Cache.Dispose();
        }

        [Test]
        public void Dispose_CustomSystemMemoryCache_TwoTimes()
        {
            Cache = new MemoryCache(new MemoryCacheSettings { CacheName = "PINO" });
            Cache.Dispose();
            Cache.Dispose();
        }

        [Test]
        public void Dispose_CustomSystemMemoryCache_ObjectDisposedExceptionAfterDispose()
        {
            Cache = new MemoryCache(new MemoryCacheSettings { CacheName = "PINO" });
            Cache.Dispose();
            Assert.Throws<ObjectDisposedException>(() => { Cache.Count(); });         
        }

        #endregion SystemMemoryCache disposal

        #region Peek - Not supported

        [Test]
        public void Peek_ShouldThrowNotSupportedException()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStatic(p, k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.Peek<string>(p, k); });        
        }

        [Test]
        public void PeekIntoDefaultPartition_ShouldThrowNotSupportedException()
        {
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStaticToDefaultPartition(k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.PeekIntoDefaultPartition<string>(k); });        
        }

        [Test]
        public void PeekItem_ShouldThrowNotSupportedException()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStatic(p, k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.PeekItem<string>(p, k); });          
        }

        [Test]
        public void PeekItemIntoDefaultPartition_ShouldThrowNotSupportedException()
        {
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStaticToDefaultPartition(k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.PeekItemIntoDefaultPartition<string>(k); });        
        }

        [Test]
        public void PeekItems_AllCache_ShouldThrowNotSupportedException()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStatic(p, k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.PeekItems<string>(); });      
        }

        [Test]
        public void PeekItems_OnePartition_ShouldThrowNotSupportedException()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            Cache.AddStatic(p, k, v);
            Assert.Throws<NotSupportedException>(() => { Cache.PeekItems<string>(p); });          
        }

        #endregion

        [Test]
        public void AddStatic_WithParentKey_RemoveParentAndChildrenShouldAlsoBeRemoved()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var t = StringItems[4];

            Cache.AddStatic(p, t, t);
            Cache.AddStatic(p, k, Tuple.Create(v1, v2), new[] { t });

            Assert.That(Cache.Contains(p, t));
            Assert.That(Cache.Contains(p, k));

            Cache.Remove(p, t);

            Assert.That(!Cache.Contains(p, t));
            Assert.That(!Cache.Contains(p, k));
        }

        [Test]
        public void AddStatic_WithParentKey_RemoveParentAndChildrenShouldAlsoBeRemoved_Nested()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var t1 = StringItems[4];
            var t2 = StringItems[5];
            var t3 = StringItems[6];

            Cache.AddStatic(p, t1, t1);
            Cache.AddStatic(p, t2, t2, new[] { t1 });
            Cache.AddStatic(p, t3, t3, new[] { t2 });
            Cache.AddStatic(p, k, Tuple.Create(v1, v2), new[] { t3 });

            Assert.That(Cache.Contains(p, t1));
            Assert.That(Cache.Contains(p, t2));
            Assert.That(Cache.Contains(p, t3));
            Assert.That(Cache.Contains(p, k));

            Cache.Remove(p, t1);

            Assert.That(!Cache.Contains(p, t1));
            Assert.That(!Cache.Contains(p, t2));
            Assert.That(!Cache.Contains(p, t3));
            Assert.That(!Cache.Contains(p, k));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes_CheckItems(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.Add(TimeSpan.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
            var items = Cache.GetItems<string>();
            for (var i = 0; i < itemCount; ++i)
            {
                var s = StringItems[i];
                Assert.True(items.Count(x => x.Key == s && x.Value == s) == 1);
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
                    Cache.AddTimedToDefaultPartition(StringItems[l], StringItems[l], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    Cache.DefaultPartitionContains(StringItems[l]);
                });
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Factory.StartNew(() =>
                {
                    Cache.AddTimedToDefaultPartition(StringItems[l], StringItems[l], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
                    Cache.DefaultPartitionContains(StringItems[l]);
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
            Cache.AddTimedToDefaultPartition(StringItems[0], StringItems[1], Cache.Clock.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[1], Cache.GetFromDefaultPartition<string>(StringItems[0]).Value);
            Cache.AddTimedToDefaultPartition(StringItems[0], StringItems[2], Cache.Clock.UtcNow.AddMinutes(10));
            Assert.AreEqual(StringItems[2], Cache.GetFromDefaultPartition<string>(StringItems[0]).Value);
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Clear_ReturnsTheNumberOfItemsRemoved(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
            }

            Assert.That(Cache.Clear(), Is.EqualTo(itemCount));
            Assert.That(Cache.Count(), Is.EqualTo(0));
            Assert.That(Cache.LongCount(), Is.EqualTo(0L));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Clear_SinglePartition_ReturnsTheNumberOfItemsRemoved(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
                Cache.AddTimed(StringItems[i], StringItems[i], StringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
            }

            Assert.That(Cache.ClearDefaultPartition(), Is.EqualTo(itemCount));
            Assert.That(Cache.DefaultPartitionCount(), Is.EqualTo(0));
            Assert.That(Cache.DefaultPartitionLongCount(), Is.EqualTo(0L));
            Assert.That(Cache.Count(), Is.EqualTo(itemCount));
            Assert.That(Cache.LongCount(), Is.EqualTo(itemCount));
        }

        [Test]
        public void Count_EmptyCache()
        {
            Assert.AreEqual(0, Cache.Count());
            Assert.AreEqual(0, Cache.Count(StringItems[0]));
            Assert.AreEqual(0, Cache.LongCount());
            Assert.AreEqual(0, Cache.LongCount(StringItems[0]));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Count_FullCache_OnePartition(int itemCount)
        {
            var partition = StringItems[0];
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddStatic(partition, StringItems[i], StringItems[i]);
            }
            Assert.AreEqual(itemCount, Cache.Count());
            Assert.AreEqual(itemCount, Cache.Count(partition));
            Assert.AreEqual(itemCount, Cache.LongCount());
            Assert.AreEqual(itemCount, Cache.LongCount(partition));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Count_FullCache_TwoPartitions(int itemCount)
        {
            int i;
            var partition1 = StringItems[0];
            var p1Count = itemCount / 3;
            for (i = 0; i < itemCount / 3; ++i)
            {
                Cache.AddStatic(partition1, StringItems[i], StringItems[i]);
            }
            var partition2 = StringItems[1];
            for (; i < itemCount; ++i)
            {
                Cache.AddStatic(partition2, StringItems[i], StringItems[i]);
            }
            Assert.AreEqual(itemCount, Cache.Count());
            Assert.AreEqual(p1Count, Cache.Count(partition1));
            Assert.AreEqual(itemCount - p1Count, Cache.Count(partition2));
            Assert.AreEqual(itemCount, Cache.LongCount());
            Assert.AreEqual(p1Count, Cache.LongCount(partition1));
            Assert.AreEqual(itemCount - p1Count, Cache.LongCount(partition2));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Indexer_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache[Cache.Settings.DefaultPartition, StringItems[i]].HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.Get<object>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.GetFromDefaultPartition<object>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.Get<string>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.GetFromDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.GetItem<object>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.GetItemFromDefaultPartition<object>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.GetItem<string>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.GetItemFromDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_EmptyCache_Concurrent(int itemCount)
        {
            var tasks = new List<Task<Option<string>>>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = TaskHelper.RunAsync(() => Cache.GetFromDefaultPartition<string>(StringItems[l]));
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(tasks[i].Result.HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.AddMinutes(10));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var item = Cache.GetFromDefaultPartition<string>(StringItems[i]);
                Assert.AreEqual(StringItems[i], item.Value);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Get_FullCache_Outdated(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimedToDefaultPartition(StringItems[i], StringItems[i], Cache.Clock.UtcNow.Subtract(TimeSpan.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.GetFromDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        [Test]
        public void Get_LargeDataTable()
        {
            var dt = new RandomDataTableGenerator("A123", "Test", "Pi", "<3", "Pu").GenerateDataTable(LargeItemCount);
            Cache.AddStaticToDefaultPartition(dt.TableName, dt);
            var storedDt = Cache.GetFromDefaultPartition<DataTable>(dt.TableName).Value;
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
    }
}
