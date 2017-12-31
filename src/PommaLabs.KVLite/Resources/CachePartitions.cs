// File name: CachePartitions.cs
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

namespace PommaLabs.KVLite.Resources
{
    /// <summary>
    ///   List of cache partitions used inside this library.
    /// </summary>
    public static class CachePartitions
    {
        /// <summary>
        ///   The prefix used by all partitions defined in this project.
        /// </summary>
        public static string Prefix { get; } = nameof(KVLite);

        /// <summary>
        ///   Default partition, used when none has been specified.
        /// </summary>
        public static string Default { get; } = $"{Prefix}.Default";

        /// <summary>
        ///   Partition reserved for <see cref="Goodies.CachingEnumerable{T}"/>.
        /// </summary>
        public static string CachingEnumerablePrefix { get; } = $"{Prefix}.CachingEnumerables";

        /// <summary>
        ///   Partition reserved for <see cref="Microsoft.Extensions.Caching.Distributed.IDistributedCache"/>.
        /// </summary>
        public static string DistributedCache { get; } = $"{Prefix}.DistributedCache";
    }
}
