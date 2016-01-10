// File name: QueryCacheExtensions.cs
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

using System;
using System.Data.Entity;

namespace EntityFramework.Extensions
{
    /// <summary>
    ///   More caching extensions.
    /// </summary>
    public static class QueryCacheExtensions
    {
        /// <summary>
        ///   Temporarily configures the given <see cref="DbContext"/> so that caching can work. For
        ///   example, in order to make caching work we need to disable lazy loading and proxy creation.
        /// 
        ///   Once the returned object is disposed, the given <see cref="DbContext"/> is restored to
        ///   its original state. We strongly suggest to use this method with a "using" statement.
        /// </summary>
        /// <param name="dbContext">The context on which caching should be enabled.</param>
        /// <returns>An object which can be used to restore the context state.</returns>
#pragma warning disable CC0022 // Should dispose object

        public static IDisposable AsCaching(this DbContext dbContext) => new DbContextReverter(dbContext);

#pragma warning restore CC0022 // Should dispose object

        private sealed class DbContextReverter : IDisposable
        {
            private readonly DbContext _dbContext;
            private readonly bool _oldLazyLoading;
            private readonly bool _oldProxyCreation;
            private bool _disposed;

            public DbContextReverter(DbContext dbContext)
            {
                _dbContext = dbContext;

                _oldLazyLoading = dbContext.Configuration.LazyLoadingEnabled;
                dbContext.Configuration.LazyLoadingEnabled = false;

                _oldProxyCreation = dbContext.Configuration.ProxyCreationEnabled;
                dbContext.Configuration.ProxyCreationEnabled = false;
            }

            public void Dispose()
            {
                if (_disposed)
                {
                    return;
                }
                if (_dbContext != null)
                {
                    _dbContext.Configuration.LazyLoadingEnabled = _oldLazyLoading;
                    _dbContext.Configuration.ProxyCreationEnabled = _oldProxyCreation;
                }
                _disposed = true;
            }
        }
    }
}
