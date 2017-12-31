// File name: KVLiteIdentityServerServiceFactoryExtensions.cs
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

using IdentityServer3.Core.Models;
using PommaLabs.KVLite;
using PommaLabs.KVLite.IdentityServer3;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace IdentityServer3.Core.Configuration
{
    /// <summary>
    ///   Extension methods to add KVLite cache support to IdentityServer3.
    /// </summary>
    public static class KVLiteIdentityServerServiceFactoryExtensions
    {
        /// <summary>
        ///   Configures KVLite implementation of <see cref="Services.ICache{T}"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="cache">The underlying cache.</param>
        /// <returns>The factory.</returns>
        public static IdentityServerServiceFactory RegisterKVLiteCache(this IdentityServerServiceFactory factory, IAsyncCache cache) => factory.RegisterKVLiteCache(cache, null);

        /// <summary>
        ///   Configures KVLite implementation of <see cref="Services.ICache{T}"/>.
        /// </summary>
        /// <param name="factory">The factory.</param>
        /// <param name="cache">The underlying cache.</param>
        /// <param name="changeOptions">Can be used to customize options.</param>
        /// <returns>The factory.</returns>
        public static IdentityServerServiceFactory RegisterKVLiteCache(this IdentityServerServiceFactory factory, IAsyncCache cache, Action<KVLiteCacheOptions> changeOptions)
        {
            var options = new KVLiteCacheOptions();
            changeOptions?.Invoke(options);

            factory.Register(new Registration<IAsyncCache>(cache));
            factory.Register(new Registration<KVLiteCacheOptions>(options));

            factory.ConfigureClientStoreCache(new Registration<Services.ICache<Client>, KVLiteCache<Client>>());
            factory.ConfigureScopeStoreCache(new Registration<Services.ICache<IEnumerable<Scope>>, KVLiteCache<IEnumerable<Scope>>>());
            factory.ConfigureUserServiceCache(new Registration<Services.ICache<IEnumerable<Claim>>, KVLiteCache<IEnumerable<Claim>>>());

            return factory;
        }
    }
}
