// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using CodeProject.ObjectPool.Specialized;
using PommaLabs.CodeServices.Compression;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.Extensibility;
using System;
using System.Diagnostics.Contracts;
using System.IO;

namespace PommaLabs.CodeServices.Caching
{
    /// <summary>
    ///   Settings shared between <see cref="ICache"/> and <see cref="IAsyncCache"/>.
    /// </summary>
    public interface IEssentialCache : IDisposable
    {
        /// <summary>
        ///   Returns <c>true</c> if all "Peek" methods are implemented, <c>false</c> otherwise.
        /// </summary>
        [Pure]
        bool CanPeek { get; }

        /// <summary>
        ///   Gets the clock used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="SystemClock"/>.
        /// </remarks>
        [Pure]
        IClock Clock { get; }

        /// <summary>
        ///   Gets the compressor used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="DeflateCompressor"/>.
        /// </remarks>
        [Pure]
        ICompressor Compressor { get; }

        /// <summary>
        ///   Whether this cache has been disposed or not. When a cache has been disposed, no more
        ///   operations are allowed on it.
        /// </summary>
        [Pure]
        bool Disposed { get; }

        /// <summary>
        ///   The last error "swallowed" by the cache. All caches should try to swallow as much
        ///   exceptions as possible, because a failure in the cache should never harm the main
        ///   application. This is an important rule, which sometimes gets forgotten.
        ///
        ///   This property might be used to expose the last error occurred while processing cache
        ///   items. If no error has occurred, this property will simply be null.
        ///
        ///   Every error is carefully registered using an internal logger, so no information is lost
        ///   when the cache swallows the exception.
        /// </summary>
        [Pure]
        Exception LastError { get; }

        /// <summary>
        ///   The maximum number of parent keys each item can have.
        /// </summary>
        [Pure]
        int MaxParentKeyCountPerItem { get; }

        /// <summary>
        ///   The pool used to retrieve <see cref="MemoryStream"/> instances.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="MemoryStreamPool.Instance"/>.
        /// </remarks>
        [Pure]
        IMemoryStreamPool MemoryStreamPool { get; }

        /// <summary>
        ///   Gets the serializer used by the cache.
        /// </summary>
        /// <remarks>
        ///   This property belongs to the services which can be injected using the cache
        ///   constructor. If not specified, it defaults to <see cref="JsonSerializer"/>.
        /// </remarks>
        [Pure]
        ISerializer Serializer { get; }
    }
}
