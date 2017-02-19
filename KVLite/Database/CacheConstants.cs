// File name: Constants.cs
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

using CodeProject.ObjectPool.Specialized;
using Polly;
using Polly.Retry;
using PommaLabs.CodeServices.Common.Portability;
using PommaLabs.CodeServices.Compression;
using PommaLabs.CodeServices.Serialization;
using System;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters;
using System.Text.RegularExpressions;
using Troschuetz.Random;
using Troschuetz.Random.Generators;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Shared constants.
    /// </summary>
    public static class CacheConstants
    {
        /// <summary>
        ///   The prefix used by all partitions defined in this project.
        /// </summary>
        public static string PartitionPrefix { get; } = nameof(KVLite);

        /// <summary>
        ///   Default memory stream pool.
        /// </summary>
        public static IMemoryStreamPool DefaultMemoryStreamPool { get; } = MemoryStreamPool.Instance;

        /// <summary>
        ///   Default compressor.
        /// </summary>
#if NET40
        public static ICompressor DefaultCompressor { get; } = new DeflateCompressor(CompressionLevel.Optimal, CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance);
#else
        public static ICompressor DefaultCompressor { get; } = new DeflateCompressor(CompressionLevel.Fastest, CodeProject.ObjectPool.Specialized.MemoryStreamPool.Instance);
#endif

        /// <summary>
        ///   Default serializer.
        /// </summary>
        /// <remarks>
        ///   We apply many customizations to the JSON serializer, in order to achieve a small output size.
        /// </remarks>
        public static ISerializer DefaultSerializer { get; } = new JsonSerializer(new JsonSerializerSettings
        {
            ContractResolver = CodeServices.Serialization.Json.Resolvers.DefaultContractResolver.Instance,
            DateFormatHandling = Newtonsoft.Json.DateFormatHandling.IsoDateFormat,
            DefaultValueHandling = Newtonsoft.Json.DefaultValueHandling.IgnoreAndPopulate,
            Encoding = PortableEncoding.UTF8WithoutBOM,
            FloatFormatHandling = Newtonsoft.Json.FloatFormatHandling.String,
            Formatting = Newtonsoft.Json.Formatting.None,
            MissingMemberHandling = Newtonsoft.Json.MissingMemberHandling.Ignore,
            NullValueHandling = Newtonsoft.Json.NullValueHandling.Ignore,
            PreserveReferencesHandling = Newtonsoft.Json.PreserveReferencesHandling.None,
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore,
            TypeNameHandling = Newtonsoft.Json.TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
        }, MemoryStreamPool.Instance);

        /// <summary>
        ///   Creates a random number generator.
        /// </summary>
        public static IGenerator CreateRandomGenerator() => new XorShift128Generator();

        #region Internal constants

#if NET40
        internal const System.Runtime.CompilerServices.MethodImplOptions MethodImplOptions = default(System.Runtime.CompilerServices.MethodImplOptions);
#else
        internal const System.Runtime.CompilerServices.MethodImplOptions MethodImplOptions = System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining;
#endif

        /// <summary>
        ///   Used to validate SQL names.
        /// </summary>
        internal static Regex IsValidSqlNameRegex { get; } = new Regex("[a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        ///   Retry policy for DB cache operations.
        /// </summary>
        internal static RetryPolicy RetryPolicy { get; } = Policy
            .Handle<Exception>()
            .WaitAndRetry(3, i => TimeSpan.FromMilliseconds(10 * i * i));

#if !NET40

        /// <summary>
        ///   Retry policy for DB cache operations.
        /// </summary>
        internal static RetryPolicy AsyncRetryPolicy { get; } = Policy
            .Handle<Exception>()
            .WaitAndRetryAsync(3, i => TimeSpan.FromMilliseconds(10 * i * i));

#endif

        #endregion Internal constants
    }
}
