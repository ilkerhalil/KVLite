// File name: AbstractQueryCacheProviderTests.cs
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

#if HAS_EF

using EntityFramework.Caching;
using EntityFramework.Extensions;
using NUnit.Framework;
using PommaLabs.KVLite.EntityFramework;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests.EntityFramework
{
    //[Parallelizable(ParallelScope.Children)] <-- Cannot run in parallel due to NMemory issue (https://github.com/zzzprojects/effort/issues/33)
    internal abstract class AbstractQueryCacheProviderTests : AbstractTests
    {
        private const string Pino = "PINO";
        private const string Gino = "GINO";
        private const string Nino = "NINO";

        private DbConnection _dbConnection;
        private TestDbContext _dbContext;

        static AbstractQueryCacheProviderTests()
        {
            Effort.Provider.EffortProviderConfiguration.RegisterProvider();
        }

        #region Setup/Teardown

        protected ICache Cache;

        [SetUp]
        public virtual void SetUp()
        {
            Cache.Clear();
            QueryCacheProvider.Register(Cache);

            _dbConnection = Effort.DbConnectionFactory.CreateTransient();
            _dbConnection.Open();
            _dbContext = new TestDbContext(_dbConnection);

            // Add some standard items.
            using (var ctx = new TestDbContext(_dbConnection))
            {
                System.Data.Entity.Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());
                ctx.Database.Initialize(true);

                ctx.TestEntities1.AddRange(new[]
                {
                    new TestEntity1 { Id = 1, Name = Pino },
                    new TestEntity1 { Id = 2, Name = Gino },
                    new TestEntity1 { Id = 3, Name = Nino }
                });
                ctx.SaveChanges();
            }
        }

        [TearDown]
        public virtual void TearDown()
        {
            try
            {
                _dbContext?.Dispose();
                _dbContext = null;

                _dbConnection?.Dispose();
                _dbConnection = null;

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

        [Test]
        public void FromCache_ItemsAreAvaialable_OneQueryWithTags_ExpireOneTag()
        {
            using (_dbContext.AsCaching())
            {
                var inis = _dbContext.TestEntities1.FromCache(tags: new[] { Pino, Gino, Nino });
                Assert.That(inis, Is.Not.Null);
                Assert.That(inis.Count(), Is.EqualTo(3));
                Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(4));

                CacheManager.Current.Expire(Pino);
                Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(2));
            }
        }

        [Test]
        public void FromCache_ItemsAreAvaialable_DifferentQueries()
        {
            using (_dbContext.AsCaching())
            {
                var inis = _dbContext.TestEntities1.FromCache();
                Assert.That(inis, Is.Not.Null);
                Assert.That(inis.Count(), Is.EqualTo(3));
                Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(1));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = _dbContext.TestEntities1.Where(te => te.Name == Pino || te.Name == Nino).FromCache();
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(2));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(0));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                    Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(2));
                }
            }
        }

        [Test]
        public void FromCache_ItemsAreAvaialable_DifferentQueries_WithTags()
        {
            using (_dbContext.AsCaching())
            {
                var inis = _dbContext.TestEntities1.FromCache(tags: new[] { Pino, Gino, Nino });
                Assert.That(inis, Is.Not.Null);
                Assert.That(inis.Count(), Is.EqualTo(3));
                Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(4));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = _dbContext.TestEntities1.Where(te => te.Name == Pino || te.Name == Nino).FromCache(tags: new[] { Pino, Nino });
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(2));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(0));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));
                    Assert.That(Cache.GetItems<object>().Count, Is.EqualTo(5));
                }
            }
        }

        [Test]
        public void FromCache_ItemsAreAvaialable()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var inis = _dbContext.TestEntities1.FromCache();
                Assert.That(inis, Is.Not.Null);
                Assert.That(inis.Count(), Is.EqualTo(3));
                Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = _dbContext.TestEntities1.FromCache();
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(3));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public void FromCacheFirstOrDefault_ItemIsAvailable()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pino = _dbContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefault();
                Assert.That(pino, Is.Not.Null);
                Assert.That(pino.Name, Is.EqualTo(Pino));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    pino = anotherContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefault();
                    Assert.That(pino, Is.Not.Null);
                    Assert.That(pino.Name, Is.EqualTo(Pino));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public void FromCacheFirstOrDefault_ItemIsAvailable_TwoQueries()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pino = _dbContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefault();
                Assert.That(pino, Is.Not.Null);
                Assert.That(pino.Name, Is.EqualTo(Pino));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    var gino = anotherContext.TestEntities1.Where(te => te.Name == Gino).FromCacheFirstOrDefault();
                    Assert.That(gino, Is.Not.Null);
                    Assert.That(gino.Name, Is.EqualTo(Gino));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task FromCache_ItemsAreAvaialable_Async()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var inis = await _dbContext.TestEntities1.FromCacheAsync();
                Assert.That(inis, Is.Not.Null);
                Assert.That(inis.Count(), Is.EqualTo(3));
                Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = await _dbContext.TestEntities1.FromCacheAsync();
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(3));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task FromCacheFirstOrDefault_ItemIsAvailable_Async()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pino = await _dbContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefaultAsync();
                Assert.That(pino, Is.Not.Null);
                Assert.That(pino.Name, Is.EqualTo(Pino));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    pino = await anotherContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefaultAsync();
                    Assert.That(pino, Is.Not.Null);
                    Assert.That(pino.Name, Is.EqualTo(Pino));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public async System.Threading.Tasks.Task FromCacheFirstOrDefault_ItemIsAvailable_TwoQueries_Async()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pino = await _dbContext.TestEntities1.Where(te => te.Name == Pino).FromCacheFirstOrDefaultAsync();
                Assert.That(pino, Is.Not.Null);
                Assert.That(pino.Name, Is.EqualTo(Pino));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    var gino = await anotherContext.TestEntities1.Where(te => te.Name == Gino).FromCacheFirstOrDefaultAsync();
                    Assert.That(gino, Is.Not.Null);
                    Assert.That(gino.Name, Is.EqualTo(Gino));
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public void FromCacheFirstOrDefault_ItemIsNotAvailable()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pinoGino = _dbContext.TestEntities1.Where(te => te.Name == Pino + Gino).FromCacheFirstOrDefault();
                Assert.That(pinoGino, Is.Null);

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    pinoGino = _dbContext.TestEntities1.Where(te => te.Name == Pino + Gino).FromCacheFirstOrDefault();
                    Assert.That(pinoGino, Is.Null);
                }
            }
            Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.True);
            Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.True);
        }

        [Test]
        public void FromCache_Paging_ShouldReturnDifferentItemsOnSamePageWhenChangingQuery()
        {
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 101, Name = "@A" });
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 102, Name = "@B" });
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 103, Name = "@C" });
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 104, Name = "#A" });
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 105, Name = "#B" });
            _dbContext.TestEntities1.Add(new TestEntity1 { Id = 106, Name = "#C" });
            _dbContext.SaveChanges();

            using (_dbContext.AsCaching())
            {
                var e1 = _dbContext.TestEntities1
                    .Where(e => e.Id > 100)
                    .Where(e => e.Name.Contains("@"))
                    .OrderBy(e => e.Id).Skip(1).Take(2).FromCache().ToArray();

                Assert.That(e1[0].Id, Is.EqualTo(102));
                Assert.That(e1[0].Name, Is.EqualTo("@B"));
                Assert.That(e1[1].Id, Is.EqualTo(103));
                Assert.That(e1[1].Name, Is.EqualTo("@C"));

                var e2 = _dbContext.TestEntities1
                    .Where(e => e.Id > 100)
                    .Where(e => e.Name.Contains("#"))
                    .OrderBy(e => e.Id).Skip(1).Take(2).FromCache().ToArray();

                Assert.That(e2[0].Id, Is.EqualTo(105));
                Assert.That(e2[0].Name, Is.EqualTo("#B"));
                Assert.That(e2[1].Id, Is.EqualTo(106));
                Assert.That(e2[1].Name, Is.EqualTo("#C"));

                var e3 = _dbContext.TestEntities1
                    .Where(e => e.Id > 100)
                    .OrderBy(e => e.Id).Skip(2).Take(2).FromCache().ToArray();

                Assert.That(e3[0].Id, Is.EqualTo(103));
                Assert.That(e3[0].Name, Is.EqualTo("@C"));
                Assert.That(e3[1].Id, Is.EqualTo(104));
                Assert.That(e3[1].Name, Is.EqualTo("#A"));
            }
        }

        public sealed class TestDbContext : DbContext
        {
            public TestDbContext(DbConnection connection)
                : base(connection, false)
            {
            }

            public DbSet<TestEntity1> TestEntities1 { get; set; }
        }

        public class TestEntity1
        {
            [Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
            public virtual int Id { get; set; }

            public virtual string Name { get; set; }
        }
    }
}

#endif
