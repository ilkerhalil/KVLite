// File name: QueryCacheProviderTests.cs
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
                    new TestEntity1 { Id = 1, Name = "PINO" },
                    new TestEntity1 { Id = 2, Name = "GINO" },
                    new TestEntity1 { Id = 3, Name = "NINO" },
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
        }

        [Test]
        public void FromCacheFirstOrDefault_ItemIsAvailable()
        {
            using (_dbContext.AsCaching())
            {
                Assert.That(_dbContext.Configuration.ProxyCreationEnabled, Is.False);
                Assert.That(_dbContext.Configuration.LazyLoadingEnabled, Is.False);

                var pino = _dbContext.TestEntities1.Where(te => te.Name == "PINO").FromCacheFirstOrDefault();
                Assert.That(pino, Is.Not.Null);

                using (var anotherContext = new TestDbContext(_dbConnection))
                using (anotherContext.AsCaching()) 
                {
                    pino = anotherContext.TestEntities1.Where(te => te.Name == "PINO").FromCacheFirstOrDefault();
                    Assert.That(pino, Is.Not.Null);
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