﻿// File name: SqlServerServiceCollectionExtensions.cs
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

using Microsoft.Extensions.Caching.Distributed;
using PommaLabs.KVLite;
using PommaLabs.KVLite.SqlServer;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///   Registrations for SQL Server KVLite services.
    /// </summary>
    public static class SqlServerServiceCollectionExtensions
    {
        /// <summary>
        ///   Registers <see cref="SqlServerCache"/> as singleton implementation for
        ///   <see cref="ICache"/>, <see cref="IAsyncCache"/>, <see cref="IDistributedCache"/>.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <returns>Modified services collection.</returns>
        public static IServiceCollection AddKVLiteSqlServerCache(this IServiceCollection services) => services.AddKVLiteSqlServerCache(null);

        /// <summary>
        ///   Registers <see cref="SqlServerCache"/> as singleton implementation for
        ///   <see cref="ICache"/>, <see cref="IAsyncCache"/>, <see cref="IDistributedCache"/>.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <param name="changeSettings">Can be used to customize settings.</param>
        /// <returns>Modified services collection.</returns>
        public static IServiceCollection AddKVLiteSqlServerCache(this IServiceCollection services, Action<SqlServerCacheSettings> changeSettings)
        {
            var settings = new SqlServerCacheSettings();
            changeSettings?.Invoke(settings);

            var ext = services.GetKVLiteExtensionServices();

#pragma warning disable CC0022 // Should dispose object
            return services.AddKVLiteCache(new SqlServerCache(settings, ext.Serializer, ext.Compressor, ext.Clock, ext.Random));
#pragma warning restore CC0022 // Should dispose object
        }
    }
}
