// File name: CacheItem.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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

using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using PommaLabs.KVLite.Utilities;
using PommaLabs.KVLite.Utilities.Extensions;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   An item that is stored in the cache.
    /// </summary>
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public class CacheItem : EquatableObject<CacheItem>
    {
        #region Public Properties

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 0)]
        public string Partition { get; set; }

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 1)]
        public string Key { get; set; }

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 2)]
        public object Value { get; set; }

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 3)]
        public DateTime UtcCreation { get; set; }

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 4)]
        public DateTime? UtcExpiry { get; set; }

        /// <summary>
        ///   </summary>
        [JsonProperty(Order = 5)]
        public TimeSpan? Interval { get; set; }

        #endregion Public Properties

        #region EquatableObject<CacheItem> Members

        /// <summary>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </summary>
        /// <returns>
        ///   Returns all property (or field) values, along with their names, so that they can be
        ///   used to produce a meaningful <see cref="M:PommaLabs.FormattableObject.ToString"/>.
        /// </returns>
        protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return GKeyValuePair.Create("Partition", Partition.SafeToString());
            yield return GKeyValuePair.Create("Key", Key.SafeToString());
            yield return GKeyValuePair.Create("UtcExpiry", UtcExpiry.SafeToString());
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

        #endregion EquatableObject<CacheItem> Members
    }

    /// <summary>
    ///   A cache item whose value is typed.
    /// </summary>
    /// <typeparam name="TVal">The type of the value.</typeparam>
    [Serializable, JsonObject(MemberSerialization.OptIn)]
    public sealed class CacheItem<TVal> : CacheItem
    {
        #region Public Properties

        /// <summary>
        ///   The typed value.
        /// </summary>
        [JsonProperty(Order = 2)]
        public new Option<TVal> Value { get; set; }

        #endregion Public Properties
    }
}