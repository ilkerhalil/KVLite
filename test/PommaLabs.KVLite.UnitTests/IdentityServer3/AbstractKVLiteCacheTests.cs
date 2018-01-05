// File name: AbstractKVLiteCacheTests.cs
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

#if HAS_ASPNET

using Ninject;
using NUnit.Framework;
using PommaLabs.KVLite.IdentityServer3;
using Shouldly;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.UnitTests.IdentityServer3
{
    [Category(nameof(IdentityServer3))]
    [NonParallelizable]
    internal abstract class AbstractKVLiteCacheTests<TBackingCache> : AbstractTests
        where TBackingCache : IAsyncCache
    {
        #region Setup/Teardown

        protected KVLiteCache<string> Cache;

        [SetUp]
        public virtual async Task SetUpAsync()
        {
            Cache = new KVLiteCache<string>(Kernel.Get<TBackingCache>(), new KVLiteCacheOptions());
            await Cache.BackingCache.ClearAsync();
        }

        [TearDown]
        public virtual async Task TearDownAsync()
        {
            try
            {
                await Cache?.BackingCache?.ClearAsync();
            }
            finally
            {
                Cache = null;
            }
        }

        #endregion Setup/Teardown

        public async Task ShouldReturnNullWhenRequestedItemDoesNotExist()
        {
            const string notExistingKey = nameof(notExistingKey);

            var result = await Cache.GetAsync(notExistingKey);

            result.ShouldBeNull();
        }

        public async Task ShouldReturnAnItemWhenItExists()
        {
            const string existingKey = nameof(existingKey);
            const string existingValue = nameof(existingValue);
            await Cache.SetAsync(existingKey, existingValue);

            var result = await Cache.GetAsync(existingKey);

            result.ShouldNotBeNull();
            result.ShouldBe(existingValue);
        }
    }
}

#endif
