// File name: AbstractCacheTests.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

using NodaTime;
using NodaTime.Testing;
using NUnit.Framework;
using PommaLabs.KVLite.Database;
using PommaLabs.KVLite.Extensibility;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.UnitTests
{
    [TestFixture]
    internal abstract class AbstractCacheTests<TSettings, TConnection> : AbstractTests
        where TSettings : DbCacheSettings<TSettings, TConnection>
        where TConnection : DbConnection
    {
        #region Setup/Teardown

        protected DbCache<TSettings, TConnection> Cache;

        [SetUp]
        public virtual void SetUp()
        {
            Cache.Clear();
        }

        [TearDown]
        public virtual void TearDown()
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

        #region Constants

        private const int LargeItemCount = 1000;

        private const int MediumItemCount = 100;

        private const int SmallItemCount = 10;

        private const int MinItem = 10000;

        protected readonly List<string> StringItems = Enumerable
            .Range(MinItem, LargeItemCount)
            .Select(x => x.ToString(CultureInfo.InvariantCulture))
            .ToList();

        #endregion Constants

        #region Tuple serialization

        [Test]
        public void AddSliding_TupleType()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var i = Duration.FromMinutes(10);

            Cache.AddSliding(p, k, Tuple.Create(p, k, v), i);
            var info = Cache.GetItem<Tuple<string, string, string>>(p, k).Value;

            info.ShouldNotBeNull();
            info.Value.Item1.ShouldBe(p);
            info.Value.Item2.ShouldBe(k);
            info.Value.Item3.ShouldBe(v);
        }

        #endregion Tuple serialization

        #region Parent keys management

        [Test]
        public void AddSliding_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.AddSliding(p, k, v, Duration.FromDays(1), t); });
        }

        [Test]
        public void AddStatic_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.AddStatic(p, k, v, t); });
        }

        [Test]
        public void AddTimed_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.AddTimed(p, k, v, Duration.FromDays(1), t); });
        }

        [Test]
        public void GetOrAddSliding_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.GetOrAddSliding(p, k, () => v, Duration.FromDays(1), t); });
        }

        [Test]
        public void GetOrAddStatic_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.GetOrAddStatic(p, k, () => v, t); });
        }

        [Test]
        public void GetOrAddTimed_TooManyParentKeys()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v = StringItems[2];
            var t = new string[Cache.MaxParentKeyCountPerItem + 1];
            Assert.Throws<NotSupportedException>(() => { Cache.GetOrAddTimed(p, k, () => v, Duration.FromDays(1), t); });
        }

        #endregion Parent keys management

        [Test]
        public void AddSliding_NullKey()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddSliding(null, StringItems[1], Duration.FromSeconds(10)); });
        }

        [Test]
        public void AddSliding_NullPartition()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddSliding(null, StringItems[0], StringItems[1], Duration.FromSeconds(10)); });
        }

        [Test]
        public void AddSliding_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = Duration.FromMinutes(10);

            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void AddSliding_RightInfo_WithParentKey()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var t = StringItems[4];
            var i = Duration.FromMinutes(10);

            Cache.AddStatic(p, t, t);
            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i, new[] { t });

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);

            Assert.That(info.ParentKeys.Count, Is.EqualTo(1));
            Assert.That(info.ParentKeys, Contains.Item(t));
        }

        [Test]
        public void GetOrAddSliding_ItemMissing_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = Duration.FromMinutes(10);

            var r = Cache.GetOrAddSliding(p, k, () => Tuple.Create(v1, v2), i);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void GetOrAddSliding_ItemAvailable_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = Duration.FromMinutes(10);

            Cache.GetOrAddSliding(p, k, () => Tuple.Create(v1, v2), i);

            // Try to add again, should not work.
            var r = Cache.GetOrAddSliding(p, k, () => Tuple.Create(v2, v1), i);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void AddSliding_TwoTimes_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = Duration.FromMinutes(10);

            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i);
            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void AddSliding_TwoTimes_RightInfo_DifferentValue()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var i = Duration.FromMinutes(10);

            Cache.AddSliding(p, k, Tuple.Create(v1, v2), i);
            Cache.AddSliding(p, k, Tuple.Create(v2, v1), i);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v2, info.Value.Item1);
            Assert.AreEqual(v1, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(i, info.Interval);
        }

        [Test]
        public void AddStatic_NullKey()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddStatic(null, StringItems[1]); });
        }

        [Test]
        public void AddStatic_NullPartition()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddStatic(null, StringItems[0], StringItems[1]); });
        }

        [Test]
        public void AddStatic_NullValue()
        {
            Cache.AddStatic(StringItems[0], (object) null);
        }

        [Test]
        public void AddStatic_WithParentKey_RemoveParentAndChildShouldAlsoBeRemoved()
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
        public void AddStatic_WithParentKey_RemoveParentAndChildrenShouldAlsoBeRemoved()
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
            Cache.AddStatic(p, t3, t3, new[] { t1 });
            Cache.AddStatic(p, k, Tuple.Create(v1, v2), new[] { t1 });

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

        [Test]
        public void AddStatic_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];

            Cache.AddStatic(p, k, Tuple.Create(v1, v2));

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.FromDays(Cache.Settings.StaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddStatic_RightInfo_WithParentKey()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var t = StringItems[4];

            Cache.AddStatic(p, t, t);
            Cache.AddStatic(p, k, Tuple.Create(v1, v2), new[] { t });

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.FromDays(Cache.Settings.StaticIntervalInDays), info.Interval);

            Assert.That(info.ParentKeys.Count, Is.EqualTo(1));
            Assert.That(info.ParentKeys, Contains.Item(t));
        }

        [Test]
        public void GetOrAddStatic_MissingItem_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];

            var r = Cache.GetOrAddStatic(p, k, () => Tuple.Create(v1, v2));

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.FromDays(Cache.Settings.StaticIntervalInDays), info.Interval);
        }

        [Test]
        public void GetOrAddStatic_ItemAvailable_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];

            Cache.GetOrAddStatic(p, k, () => Tuple.Create(v1, v2));

            // Try to add again, should not work.
            var r = Cache.GetOrAddStatic(p, k, () => Tuple.Create(v2, v1));

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.FromDays(Cache.Settings.StaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddStatic_TwoTimes_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];

            Cache.AddStatic(p, k, Tuple.Create(v1, v2));
            Cache.AddStatic(p, k, Tuple.Create(v1, v2));

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.FromDays(Cache.Settings.StaticIntervalInDays), info.Interval);
        }

        [Test]
        public void AddTimed_HugeValue()
        {
            var k = StringItems[1];
            var v = new byte[20000];
            Cache.AddTimed(k, v, Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            var info = Cache.GetItemFromDefaultPartition<byte[]>(k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v, info.Value);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void AddTimed_NullKey()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddTimed(null, StringItems[1], Cache.Clock.GetCurrentInstant()); });
        }

        [Test]
        public void AddTimed_NullPartition()
        {
            Assert.Throws<ArgumentNullException>(() => { Cache.AddTimed(null, StringItems[0], StringItems[1], Cache.Clock.GetCurrentInstant()); });
        }

        [Test]
        public void AddTimed_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);

            Cache.AddTimed(p, k, Tuple.Create(v1, v2), e);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void AddTimed_RightInfo_WithParentKey()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var t = StringItems[4];
            var e = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);

            Cache.AddStatic(p, t, t);
            Cache.AddTimed(p, k, Tuple.Create(v1, v2), e, new[] { t });

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);

            Assert.That(info.ParentKeys.Count, Is.EqualTo(1));
            Assert.That(info.ParentKeys, Contains.Item(t));
        }

        [Test]
        public void AddTimed_WithDuration_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var l = Duration.FromMinutes(10);

            Cache.AddTimed(p, k, Tuple.Create(v1, v2), l);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            var e = Cache.Clock.GetCurrentInstant().Plus(l);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void GetOrAddTimed_MissingItem_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);

            var r = Cache.GetOrAddTimed(p, k, () => Tuple.Create(v1, v2), e);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void GetOrAddTimed_WithDuration_MissingItem_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var l = Duration.FromMinutes(10);

            var r = Cache.GetOrAddTimed(p, k, () => Tuple.Create(v1, v2), l);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);

            var e = Cache.Clock.GetCurrentInstant().Plus(l);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public async Task GetOrAddTimedAsync_WithDuration_MissingItem_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var l = Duration.FromMinutes(10);

            var r = await Cache.GetOrAddTimedAsync(p, k, () => Task.FromResult(Tuple.Create(v1, v2)), l);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);

            var e = Cache.Clock.GetCurrentInstant().Plus(l);
            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void GetOrAddTimed_ItemAvailable_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);

            Cache.GetOrAddTimed(p, k, () => Tuple.Create(v1, v2), e);

            // Try to add again, should not work.
            var r = Cache.GetOrAddTimed(p, k, () => Tuple.Create(v2, v1), e);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);
            Assert.AreEqual(v1, r.Item1);
            Assert.AreEqual(v2, r.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [Test]
        public void AddTimed_TwoTimes_RightInfo()
        {
            var p = StringItems[0];
            var k = StringItems[1];
            var v1 = StringItems[2];
            var v2 = StringItems[3];
            var e = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);

            Cache.AddTimed(p, k, Tuple.Create(v1, v2), e);
            Cache.AddTimed(p, k, Tuple.Create(v1, v2), e);

            var info = Cache.GetItem<Tuple<string, string>>(p, k).Value;
            Assert.IsNotNull(info);
            Assert.AreEqual(p, info.Partition);
            Assert.AreEqual(k, info.Key);
            Assert.AreEqual(v1, info.Value.Item1);
            Assert.AreEqual(v2, info.Value.Item2);

            Assert.IsNotNull(info.UtcExpiry);
            Assert.AreEqual(e.InUtc().Date, info.UtcExpiry.InUtc().Date);
            Assert.AreEqual(e.InUtc().Hour, info.UtcExpiry.InUtc().Hour);
            Assert.AreEqual(e.InUtc().Minute, info.UtcExpiry.InUtc().Minute);
            Assert.AreEqual(e.InUtc().Second, info.UtcExpiry.InUtc().Second);

            Assert.AreEqual(Duration.Zero, info.Interval);
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void AddTimed_TwoTimes(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Plus(Duration.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Plus(Duration.FromMinutes(10)));
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
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Plus(Duration.FromMinutes(10)));
                Assert.True(Cache.DefaultPartitionContains(StringItems[i]));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Plus(Duration.FromMinutes(10)));
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
                    Cache.AddTimed(StringItems[l], StringItems[l], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
                    Cache.DefaultPartitionContains(StringItems[l]);
                });
                tasks.Add(task);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Factory.StartNew(() =>
                {
                    Cache.AddTimed(StringItems[l], StringItems[l], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
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
            Cache.AddTimed(StringItems[0], StringItems[1], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            Assert.AreEqual(StringItems[1], Cache.GetFromDefaultPartition<string>(StringItems[0]).Value);
            Cache.AddTimed(StringItems[0], StringItems[2], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            Assert.AreEqual(StringItems[2], Cache.GetFromDefaultPartition<string>(StringItems[0]).Value);
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
            var tasks = new List<Task<CacheResult<string>>>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Run(() => Cache.GetFromDefaultPartition<string>(StringItems[l]));
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
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
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
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
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
            Cache.AddStatic(dt.TableName, dt);
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

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddSliding_InvalidTime(int itemCount)
        {
            AddSliding(Cache, itemCount, Duration.FromSeconds(1));
            (Cache.Clock as FakeClock).Advance(Duration.FromSeconds(2));
            var items = new HashSet<string>(Cache.GetItems<string>().Select(i => i.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(Cache.GetItems<string>().Select(x => x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddSliding_ValidTime(int itemCount)
        {
            AddSliding(Cache, itemCount, Duration.FromHours(1));
            var items = new HashSet<string>(Cache.GetItems<string>().Select(i => i.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(Cache.GetItems<string>().Select(x => x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddStatic(int itemCount)
        {
            AddStatic(Cache, itemCount);
            var items = new HashSet<string>(Cache.GetItems<string>().Select(i => i.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(Cache.GetItems<string>().Select(x => x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddTimed_InvalidTime(int itemCount)
        {
            AddTimed(Cache, itemCount, Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
            var items = new HashSet<string>(Cache.GetItems<string>().Select(i => i.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(Cache.GetItems<string>().Select(x => x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.False(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetMany_RightItems_AfterAddTimed_ValidTime(int itemCount)
        {
            AddTimed(Cache, itemCount, Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            var items = new HashSet<string>(Cache.GetItems<string>().Select(i => i.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
            items = new HashSet<string>(Cache.GetItems<string>().Select(x => x.Value));
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.True(items.Contains(StringItems[i]));
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetItem_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var interval = Duration.FromMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddSliding(StringItems[i], StringItems[i], interval);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var item = Cache.GetItemFromDefaultPartition<string>(StringItems[i]).Value;
                Assert.IsNotNull(item);
                item.UtcExpiry.ToUnixTimeSeconds().ShouldBe((Cache.Clock.GetCurrentInstant() + interval).ToUnixTimeSeconds());
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void GetManyItems_FullSlidingCache_TimeIncreased(int itemCount)
        {
            var interval = Duration.FromMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddSliding(StringItems[i], StringItems[i], interval);
            }
            (Cache.Clock as FakeClock).Advance(Duration.FromMinutes(1));
            var items = Cache.GetItems<string>();
            for (var i = 0; i < itemCount; ++i)
            {
                var item = items[i];
                Assert.IsNotNull(item);
                item.UtcExpiry.ToUnixTimeSeconds().ShouldBe((Cache.Clock.GetCurrentInstant() + interval).ToUnixTimeSeconds());
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Clear_ReturnsTheNumberOfItemsRemoved(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
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
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
                Cache.AddTimed(StringItems[i], StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            }

            Assert.That(Cache.Clear(Cache.Settings.DefaultPartition), Is.EqualTo(itemCount));
            Assert.That(Cache.Count(Cache.Settings.DefaultPartition), Is.EqualTo(0));
            Assert.That(Cache.LongCount(Cache.Settings.DefaultPartition), Is.EqualTo(0L));
            Assert.That(Cache.Count(), Is.EqualTo(itemCount));
            Assert.That(Cache.LongCount(), Is.EqualTo(itemCount));
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues()
        {
            const int fixedValue = 64;
            for (var i = 0; i < fixedValue; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
            }
            Assert.AreEqual(0, Cache.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues()
        {
            const int fixedValue = 64;
            for (var i = 0; i < fixedValue - 1; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            }
            Assert.AreEqual(fixedValue - 1, Cache.Count());
            Assert.AreEqual(fixedValue - 1, Cache.Count(CacheReadMode.IgnoreExpiryDate));

            // Advance the clock, in order to make items not valid.
            (Cache.Clock as FakeClock).Advance(Duration.FromMinutes(15));

            // Add a new item, and then trigger a soft cleanup.
            Cache.AddTimed(StringItems[0], StringItems[0], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            Cache.Clear(CacheReadMode.ConsiderExpiryDate);

            Assert.AreEqual(1, Cache.Count());
            Assert.AreEqual(1, Cache.Count(CacheReadMode.IgnoreExpiryDate));
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_InvalidValues_Async()
        {
            const int fixedValue = 64;
            Parallel.For(0, fixedValue, i =>
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
            });
            Assert.AreEqual(0, Cache.Count());
        }

        [Test]
        public void Clean_AfterFixedNumberOfInserts_ValidValues_Async()
        {
            const int fixedValue = 64;
            Parallel.For(0, fixedValue - 1, i =>
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            });
            Assert.AreEqual(fixedValue - 1, Cache.Count());
            Assert.AreEqual(fixedValue - 1, Cache.Count(CacheReadMode.IgnoreExpiryDate));

            // Advance the clock, in order to make items not valid.
            (Cache.Clock as FakeClock).Advance(Duration.FromMinutes(15));

            // Add a new item, and then trigger a soft cleanup.
            Cache.AddTimed(StringItems[0], StringItems[0], Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10));
            Cache.Clear(CacheReadMode.ConsiderExpiryDate);

            Assert.AreEqual(1, Cache.Count());
            Assert.AreEqual(1, Cache.Count(CacheReadMode.IgnoreExpiryDate));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.Peek<object>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.PeekIntoDefaultPartition<object>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.Peek<string>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.PeekIntoDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.PeekItem<object>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.PeekItemIntoDefaultPartition<object>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void PeekItem_Typed_EmptyCache(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.PeekItem<string>(Cache.Settings.DefaultPartition, StringItems[i]).HasValue);
                Assert.IsFalse(Cache.PeekItemIntoDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_EmptyCache_Concurrent(int itemCount)
        {
            var tasks = new List<Task<CacheResult<string>>>();
            for (var i = 0; i < itemCount; ++i)
            {
                var l = i;
                var task = Task.Run(() => Cache.PeekIntoDefaultPartition<string>(StringItems[l]));
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
        public void Peek_FullCache_ExpiryNotChanged(int itemCount)
        {
            var expiryDate = Cache.Clock.GetCurrentInstant() + Duration.FromMinutes(10);
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], expiryDate);
            }
            for (var i = 0; i < itemCount; ++i)
            {
                var value = Cache.PeekIntoDefaultPartition<string>(StringItems[i]);
                Assert.AreEqual(StringItems[i], value.Value);
                var item = Cache.PeekItemIntoDefaultPartition<string>(StringItems[i]).Value;
                Assert.AreEqual(expiryDate.InUtc().Date, item.UtcExpiry.InUtc().Date);
                Assert.AreEqual(expiryDate.InUtc().Hour, item.UtcExpiry.InUtc().Hour);
                Assert.AreEqual(expiryDate.InUtc().Minute, item.UtcExpiry.InUtc().Minute);
                Assert.AreEqual(expiryDate.InUtc().Second, item.UtcExpiry.InUtc().Second);
            }
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Peek_FullCache_Outdated(int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                Cache.AddTimed(StringItems[i], StringItems[i], Cache.Clock.GetCurrentInstant().Minus(Duration.FromMinutes(10)));
            }
            for (var i = 0; i < itemCount; ++i)
            {
                Assert.IsFalse(Cache.PeekIntoDefaultPartition<string>(StringItems[i]).HasValue);
            }
        }

        #region Serialization

        [Test]
        //[ExpectedException(typeof(ArgumentException))] <-- Not thrown anymore with JsonSerializer
        public void AddStatic_NotSerializableValue()
        {
            Cache.AddStatic(StringItems[0], new NotSerializableClass());
        }

        [Test]
        //[ExpectedException(typeof(ArgumentException))] <-- Not thrown anymore with JsonSerializer
        public void AddStatic_DataContractValue()
        {
            Cache.AddStatic(StringItems[0], new DataContractClass());
        }

        #endregion Serialization

        #region BCL Collections

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_Dictionary(int itemCount)
        {
            var l = Enumerable.Range(1, itemCount).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture));
            Cache.AddStatic("dict", l);
            var lc = Cache.GetFromDefaultPartition<Dictionary<int, string>>("dict").Value;
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_List(int itemCount)
        {
            var l = new List<int>(Enumerable.Range(1, itemCount));
            Cache.AddStatic("list", l);
            var lc = Cache.GetFromDefaultPartition<List<int>>("list").Value;
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_HashSet(int itemCount)
        {
            var l = new HashSet<int>(Enumerable.Range(1, itemCount));
            Cache.AddStatic("set", l);
            var lc = Cache.GetFromDefaultPartition<HashSet<int>>("set").Value;
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_SortedSet(int itemCount)
        {
            var l = new SortedSet<int>(Enumerable.Range(1, itemCount));
            Cache.AddStatic("set", l);
            var lc = Cache.GetFromDefaultPartition<SortedSet<int>>("set").Value;
            Assert.True(l.SequenceEqual(lc));
        }

        [TestCase(SmallItemCount)]
        [TestCase(MediumItemCount)]
        [TestCase(LargeItemCount)]
        public void Collections_SortedList(int itemCount)
        {
            var l = new SortedList<int, string>(Enumerable.Range(1, itemCount).ToDictionary(i => i, i => i.ToString(CultureInfo.InvariantCulture)));
            Cache.AddStatic("list", l);
            var lc = Cache.GetFromDefaultPartition<SortedList<int, string>>("list").Value;
            Assert.True(l.SequenceEqual(lc));
        }

        #endregion BCL Collections

        #region Private Methods

        private void AddSliding(ICache instance, int itemCount, Duration interval)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddSliding(StringItems[i], StringItems[i], interval);
            }
        }

        private void AddStatic(ICache instance, int itemCount)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddStatic(StringItems[i], StringItems[i]);
            }
        }

        private void AddTimed(ICache instance, int itemCount, Instant utcTime)
        {
            for (var i = 0; i < itemCount; ++i)
            {
                instance.AddTimed(StringItems[i], StringItems[i], utcTime);
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
