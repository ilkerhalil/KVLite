﻿// File name: QueryCacheProviderTests.cs
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

using EntityFramework.Extensions;
using NUnit.Framework;
using PommaLabs.KVLite.EntityFramework;
using System.Data.Common;
using System.Data.Entity;
using System.Linq;

namespace PommaLabs.KVLite.UnitTests.EntityFramework
{
    [TestFixture]
    public sealed class QueryCacheProviderTests
    {
        const string Pino = "PINO";
        const string Gino = "GINO";
        const string Nino = "NINO";

        DbConnection _dbConnection;
        TestDbContext _dbContext;

        static QueryCacheProviderTests()
        {
            Effort.Provider.EffortProviderConfiguration.RegisterProvider();
            QueryCacheProvider.Register(VolatileCache.DefaultInstance);
        }

        [SetUp]
        public void SetUp()
        {
            VolatileCache.DefaultInstance.Clear();

            _dbConnection = Effort.DbConnectionFactory.CreateTransient();
            _dbConnection.Open();
            _dbContext = new TestDbContext(_dbConnection);

            // Add some standard items.
            using (var ctx = new TestDbContext(_dbConnection))
            {
                Database.SetInitializer(new DropCreateDatabaseAlways<TestDbContext>());
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
        public void TearDown()
        {
            _dbContext?.Dispose();
            _dbContext = null;

            _dbConnection?.Dispose();
            _dbConnection = null;

            VolatileCache.DefaultInstance.Clear();
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

                Assert.That(VolatileCache.DefaultInstance.GetItems<object>().Length, Is.EqualTo(1));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = _dbContext.TestEntities1.Where(te => te.Name == Pino || te.Name == Nino).FromCache();
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(2));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(0));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                    Assert.That(VolatileCache.DefaultInstance.GetItems<object>().Length, Is.EqualTo(2));
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

                Assert.That(VolatileCache.DefaultInstance.GetItems<object>().Length, Is.EqualTo(4));

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching())
                {
                    inis = _dbContext.TestEntities1.Where(te => te.Name == Pino || te.Name == Nino).FromCache(tags: new[] { Pino, Nino });
                    Assert.That(inis, Is.Not.Null);
                    Assert.That(inis.Count(), Is.EqualTo(2));
                    Assert.That(inis.Count(i => i.Name == Pino), Is.EqualTo(1));
                    Assert.That(inis.Count(i => i.Name == Gino), Is.EqualTo(0));
                    Assert.That(inis.Count(i => i.Name == Nino), Is.EqualTo(1));

                    Assert.That(VolatileCache.DefaultInstance.GetItems<object>().Length, Is.EqualTo(5));
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

#if !NET40

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

#endif

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
            public virtual int Id { get; set; }

            public virtual string Name { get; set; }
        }
    }
}