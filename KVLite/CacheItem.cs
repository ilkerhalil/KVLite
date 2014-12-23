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

using Newtonsoft.Json;
using PommaLabs.GRAMPA;
using PommaLabs.GRAMPA.Extensions;
using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable, JsonObject]
    public sealed class CacheItem : EquatableObject<CacheItem>
    {
        /// <summary>
        ///   </summary>
        public string Partition { get; set; }

        /// <summary>
        ///   </summary>
        public string Key { get; set; }

        /// <summary>
        ///   </summary>
        public object Value { get; set; }

        /// <summary>
        ///   </summary>
        public DateTime UtcCreation { get; set; }

        /// <summary>
        ///   </summary>
        public DateTime? UtcExpiry { get; set; }

        /// <summary>
        ///   </summary>
        public TimeSpan? Interval { get; set; }

        #region EquatableObject<CacheItem> Members

        protected override IEnumerable<GKeyValuePair<string, string>> GetFormattingMembers()
        {
            yield return GKeyValuePair.Create("Partition", Partition.SafeToString());
            yield return GKeyValuePair.Create("Key", Key.SafeToString());
            yield return GKeyValuePair.Create("UtcExpiry", UtcExpiry.SafeToString());
        }

        protected override IEnumerable<object> GetIdentifyingMembers()
        {
            yield return Partition;
            yield return Key;
        }

        #endregion
    }
}