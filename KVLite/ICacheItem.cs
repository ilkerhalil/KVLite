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

using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An item that is stored in the cache.
    /// </summary>
    /// <typeparam name="TVal">The type of the value.</typeparam>
    public interface ICacheItem<TVal>
    {
        /// <summary>
        ///   The partition corresponding to this entry.
        /// </summary>
        string Partition { get; }

        /// <summary>
        ///   The key corresponding to this entry.
        /// </summary>
        string Key { get; }

        /// <summary>
        ///   The typed value.
        /// </summary>
        TVal Value { get; }

        /// <summary>
        ///   When the cache item was created.
        /// </summary>
        DateTime UtcCreation { get; }

        /// <summary>
        ///   When the cache item will expire.
        /// </summary>
        DateTime UtcExpiry { get; }

        /// <summary>
        ///   The refresh interval, used if the item is sliding; if it is not, it equals to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        TimeSpan Interval { get; }

        /// <summary>
        ///   The parent keys of this item. If not specified, the array will be empty, but not null.
        /// </summary>
        IList<string> ParentKeys { get; }
    }
}
