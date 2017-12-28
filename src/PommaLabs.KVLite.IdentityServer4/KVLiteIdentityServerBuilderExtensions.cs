// File name: KVLiteIdentityServerBuilderExtensions.cs
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

using PommaLabs.KVLite.IdentityServer4;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///   Extension methods to add KVLite cache support to IdentityServer4.
    /// </summary>
    public static class KVLiteIdentityServerBuilderExtensions
    {
        /// <summary>
        ///   Configures KVLite implementation of <see cref="IdentityServer4.Services.ICache{T}"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <returns>The builder.</returns>
        public static IIdentityServerBuilder AddKVLiteCaching(this IIdentityServerBuilder builder) => builder.AddKVLiteCaching(null);

        /// <summary>
        ///   Configures KVLite implementation of <see cref="IdentityServer4.Services.ICache{T}"/>.
        /// </summary>
        /// <param name="builder">The builder.</param>
        /// <param name="changeOptions">Can be used to customize options.</param>
        /// <returns>The builder.</returns>
        public static IIdentityServerBuilder AddKVLiteCaching(this IIdentityServerBuilder builder, Action<KVLiteCacheOptions> changeOptions)
        {
            var options = new KVLiteCacheOptions();
            changeOptions?.Invoke(options);

            builder.Services.AddSingleton(options);
            builder.Services.AddSingleton(typeof(IdentityServer4.Services.ICache<>), typeof(KVLiteCache<>));

            return builder;
        }
    }
}
