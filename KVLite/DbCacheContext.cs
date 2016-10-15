// File name: DbCacheContext.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.Thrower;
using System.Data.Entity;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Data connection for persistent cache.
    /// </summary>
    public sealed class DbCacheContext : DbContext
    {
        private readonly IDbCacheConnectionFactory _connectionFactory;

        static DbCacheContext()
        {
            Database.SetInitializer<DbCacheContext>(null);
        }

        /// <summary>
        ///   Builds the cache context.
        /// </summary>
        /// <param name="connectionFactory">Cache connection factory.</param>
        public DbCacheContext(IDbCacheConnectionFactory connectionFactory)
            : base(connectionFactory.Create(), true)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(connectionFactory, nameof(connectionFactory));

            _connectionFactory = connectionFactory;

            Configuration.LazyLoadingEnabled = false;
            Configuration.UseDatabaseNullSemantics = true;
            Configuration.ValidateOnSaveEnabled = false;
        }

        /// <summary>
        ///   Cache items.
        /// </summary>
        public DbSet<DbCacheItem> CacheItems { get; set; }

        /// <summary>
        ///   This method is called when the model for a derived context has been initialized, but
        ///   before the model has been locked down and used to initialize the context. The default
        ///   implementation of this method does nothing, but it can be overridden in a derived class
        ///   such that the model can be further configured before it is locked down.
        /// </summary>
        /// <remarks>
        ///   Typically, this method is called only once when the first instance of a derived context
        ///   is created. The model for that context is then cached and is for all further instances
        ///   of the context in the app domain. This caching can be disabled by setting the
        ///   ModelCaching property on the given ModelBuidler, but note that this can seriously
        ///   degrade performance. More control over caching is provided through use of the
        ///   DbModelBuilder and DbContextFactory classes directly.
        /// </remarks>
        /// <param name="modelBuilder">
        ///   The builder that defines the model for the context being created.
        /// </param>
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var dbCacheItemTable = modelBuilder.Entity<DbCacheItem>();

            dbCacheItemTable
                .ToTable(_connectionFactory.CacheItemsTableName, _connectionFactory.CacheSchemaName);

            dbCacheItemTable
                .Property(x => x.Partition).HasMaxLength(_connectionFactory.MaxPartitionNameLength);

            dbCacheItemTable
                .Property(x => x.Key).HasMaxLength(_connectionFactory.MaxKeyNameLength);
        }
    }
}