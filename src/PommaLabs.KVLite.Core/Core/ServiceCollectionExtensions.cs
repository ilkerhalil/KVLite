// File name: ServiceCollectionExtensions.cs
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
using Microsoft.Extensions.DependencyInjection;
using NodaTime;
using PommaLabs.KVLite.Extensibility;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Registrations for core KVLite services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        ///   Registers specified cache as singleton implementation for <see cref="ICache"/>,
        ///   <see cref="IAsyncCache"/>, <see cref="IDistributedCache"/>.
        /// </summary>
        /// <typeparam name="TCache">Cache type.</typeparam>
        /// <typeparam name="TSettings">Cache settings type.</typeparam>
        /// <param name="services">Services collection.</param>
        /// <param name="cache">The cache that should be registered.</param>
        /// <returns>Modified services collection.</returns>
        public static IServiceCollection AddKVLiteCache<TCache, TSettings>(this IServiceCollection services, AbstractCache<TCache, TSettings> cache)
            where TCache : AbstractCache<TCache, TSettings>
            where TSettings : AbstractCacheSettings<TSettings>
        {
            if (cache != null)
            {
                services.AddSingleton<ICache>(cache);
                services.AddSingleton<ICache<TSettings>>(cache);
                services.AddSingleton<IAsyncCache>(cache);
                services.AddSingleton<IAsyncCache<TSettings>>(cache);
                services.AddSingleton<IDistributedCache>(cache);
            }
            return services;
        }

        /// <summary>
        ///   Gets extension services, if any, or returns default services.
        /// </summary>
        /// <param name="services">Services collection.</param>
        /// <returns>KVLite extension services.</returns>
        public static (ISerializer Serializer, ICompressor Compressor, IClock Clock, IRandom Random) GetKVLiteExtensionServices(this IServiceCollection services)
        {
            var provider = services.BuildServiceProvider();

            var serializer = provider.GetService<ISerializer>() ?? JsonSerializer.Instance;
            var compressor = provider.GetService<ICompressor>() ?? DeflateCompressor.Instance;
            var clock = provider.GetService<IClock>() ?? SystemClock.Instance;
            var random = provider.GetService<IRandom>() ?? new SystemRandom();

            return (serializer, compressor, clock, random);
        }
    }
}
