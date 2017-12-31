﻿// File name: MemoryOutputCacheProvider.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using PommaLabs.KVLite.Memory;

namespace PommaLabs.KVLite.WebForms
{
    /// <summary>
    ///   A custom output cache provider based on KVLite. By default, it uses the instance defined
    ///   by the property <see cref="WebCaches.Memory"/>.
    /// </summary>
    public sealed class MemoryOutputCacheProvider : AbstractOutputCacheProvider
    {
        /// <summary>
        ///   Initializes the provider using the default cache, that is, <see cref="WebCaches.Memory"/>.
        /// </summary>
        public MemoryOutputCacheProvider()
            : base(WebCaches.Memory)
        {
        }

        /// <summary>
        ///   Initializes the provider using the specified cache.
        /// </summary>
        /// <param name="cache">The cache that will be used by the provider.</param>
        public MemoryOutputCacheProvider(MemoryCache cache)
            : base(cache)
        {
        }
    }
}
