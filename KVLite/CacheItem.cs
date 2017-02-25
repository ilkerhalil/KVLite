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

using PommaLabs.CodeServices.Common;
using PommaLabs.Thrower;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An item that is stored into the cache.
    /// </summary>
    /// <typeparam name="TVal">The type of the cached value.</typeparam>
    [Serializable, DataContract]
    public sealed class CacheItem<TVal> : EquatableObject<CacheItem<TVal>>, ICacheItem<TVal>, IEquatable<ICacheItem<TVal>>
    {
        #region Construction

        /// <summary>
        ///   Builds an empty cache item.
        /// </summary>
        public CacheItem()
        {
            // To avoid NULLs.
            ParentKeys = CacheExtensions.NoParentKeys;
        }

        /// <summary>
        ///   Clones given cache item.
        /// </summary>
        /// <param name="other">The cache item to be cloned.</param>
        public CacheItem(ICacheItem<TVal> other)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(other, nameof(other));

            Partition = other.Partition;
            Key = other.Key;
            Value = other.Value;
            UtcCreation = other.UtcCreation;
            UtcExpiry = other.UtcExpiry;
            Interval = other.Interval;

            var otherPk = other.ParentKeys;
            if (otherPk != null && otherPk.Count > 0)
            {
                ParentKeys = new List<string>(otherPk);
            }
            else
            {
                ParentKeys = CacheExtensions.NoParentKeys;
            }
        }

        /// <summary>
        ///   Builds a cache item from given parameters.
        /// </summary>
        /// <param name="partition">The partition.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <param name="utcCreation">The UTC creation time.</param>
        /// <param name="utcExpiry">The UTC expiry time.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="parentKeys">Parent keys, if any. Might be null.</param>
        public CacheItem(string partition, string key, TVal value, DateTime utcCreation, DateTime utcExpiry, TimeSpan interval, IList<string> parentKeys)
        {
            Partition = partition;
            Key = key;
            Value = value;
            UtcCreation = utcCreation;
            UtcExpiry = utcExpiry;
            Interval = interval;

            if (parentKeys != null && parentKeys.Count > 0)
            {
                ParentKeys = new List<string>(parentKeys);
            }
            else
            {
                ParentKeys = CacheExtensions.NoParentKeys;
            }
        }

        #endregion Construction

        #region Public Properties

        /// <summary>
        ///   The partition corresponding to this entry.
        /// </summary>
        [DataMember(Order = 0)]
        public string Partition { get; set; }

        /// <summary>
        ///   The key corresponding to this entry.
        /// </summary>
        [DataMember(Order = 1)]
        public string Key { get; set; }

        /// <summary>
        ///   The typed value.
        /// </summary>
        [DataMember(Order = 2)]
        public TVal Value { get; set; }

        /// <summary>
        ///   When the cache item was created.
        /// </summary>
        [DataMember(Order = 3)]
        public DateTime UtcCreation { get; set; }

        /// <summary>
        ///   When the cache item will expire.
        /// </summary>
        [DataMember(Order = 4)]
        public DateTime UtcExpiry { get; set; }

        /// <summary>
        ///   The refresh interval, used if the item is sliding; if it is not, it equals to <see cref="TimeSpan.Zero"/>.
        /// </summary>
        [DataMember(Order = 5)]
        public TimeSpan Interval { get; set; }

        /// <summary>
        ///   The parent keys of this item. If not specified, the array will be empty, but not null.
        /// </summary>
        [DataMember(Order = 6)]
        public IList<string> ParentKeys { get; set; }

        #endregion Public Properties

        #region EquatableObject<CacheItem<TVal>> Members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="object.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="object.ToString"/>.
        /// </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return new KeyValuePair<string, string>(nameof(Partition), Partition.SafeToString());
            yield return new KeyValuePair<string, string>(nameof(Key), Key.SafeToString());
            yield return new KeyValuePair<string, string>(nameof(UtcExpiry), UtcExpiry.SafeToString());
        }

        /// <summary>
        ///   Gets the identifying members.
        /// </summary>
        /// <returns>The identifying members.</returns>
        protected override IEnumerable<object> GetIdentifyingMembers()
        {
            yield return Partition;
            yield return Key;
        }

        /// <summary>
        ///   Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">An object to compare with this object.</param>
        /// <returns>
        ///   <c>true</c> if the current object is equal to the <paramref name="other"/> parameter;
        ///   otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(ICacheItem<TVal> other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Partition == other.Partition && Key == other.Key;
        }

        #endregion EquatableObject<CacheItem<TVal>> Members
    }
}
