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

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   List of cache partitions used inside this library.
    /// </summary>
    internal static class CachePartitions
    {
        /// <summary>
        ///   Partition reserved for <see cref="Goodies.CachingEnumerable{T}"/>.
        /// </summary>
        public const string CachingEnumerablePrefix = "pcs.caching_enumerable";
    }
}
