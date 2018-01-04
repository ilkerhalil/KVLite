// File name: KVLiteCacheOptions.cs
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

using PommaLabs.KVLite.Resources;
using System;

namespace PommaLabs.KVLite.IdentityServer3
{
    /// <summary>
    ///   Options for <see cref="KVLiteCache{T}"/>.
    /// </summary>
    public sealed class KVLiteCacheOptions
    {
        /// <summary>
        ///   The partition used by the cache. Defaults to "KVLite.IdentityServer3".
        /// </summary>
        public string Partition { get; set; } = $"{CachePartitions.Prefix}.{nameof(IdentityServer3)}";

        /// <summary>
        ///   How long should entries be stored into the cache. Defaults to 30 minutes.
        /// </summary>
        public TimeSpan Lifetime { get; set; } = TimeSpan.FromMinutes(30);
    }
}
