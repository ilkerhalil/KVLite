// File name: CacheItem.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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

using Finsa.CodeServices.Common;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An item that is stored in the cache.
    /// </summary>
    /// <typeparam name="TVal">The type of the value.</typeparam>
    [Serializable, DataContract]
    public class CacheItem<TVal> : EquatableObject<CacheItem<TVal>>
    {
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
        public string[] ParentKeys { get; set; }

        #endregion Public Properties

        #region EquatableObject<CacheItem<TVal>> Members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </returns>
        protected override IEnumerable<KeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return KeyValuePair.Create(nameof(Partition), Partition.SafeToString());
            yield return KeyValuePair.Create(nameof(Key), Key.SafeToString());
            yield return KeyValuePair.Create(nameof(UtcExpiry), UtcExpiry.SafeToString());
        }

        /// <summary>
        ///   Gets the identifying members.
        /// </summary>
        /// <returns>The identifying members.</returns>
        protected override IEnumerable<object> GetIdentifyingMembers()
        {
            yield return Key;
            yield return Partition;
        }

        #endregion EquatableObject<CacheItem<TVal>> Members
    }
}
