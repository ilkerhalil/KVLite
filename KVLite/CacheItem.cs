//
// CacheItem.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using PommaLabs.KVLite.Core;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   TODO
    /// </summary>
    [Serializable]
    public sealed class CacheItem
    {
        /// <summary>
        ///   TODO
        /// </summary>
        public CacheItem() {}

        internal CacheItem(DbCacheItem original)
        {
            Partition = original.Partition;
            Key = original.Key;
            Value = BinarySerializer.DeserializeObject(original.SerializedValue);
            UtcCreation = DateTime.MinValue.AddTicks(original.UtcCreation);
            UtcExpiry = original.UtcExpiry == null ? new DateTime?() : DateTime.MinValue.AddTicks(original.UtcExpiry.Value);
            Interval = original.Interval == null ? new TimeSpan?() : TimeSpan.FromTicks(original.Interval.Value);
        }

        public string Partition { get; set; }
        public string Key { get; set; }
        public object Value { get; set; }
        public DateTime UtcCreation { get; set; }
        public DateTime? UtcExpiry { get; set; }
        public TimeSpan? Interval { get; set; }
    }

    [Serializable]
    internal sealed class DbCacheItem
    {
        public string Partition { get; set; }
        public string Key { get; set; }
        public byte[] SerializedValue { get; set; }
        public long UtcCreation { get; set; }
        public long? UtcExpiry { get; set; }
        public long? Interval { get; set; }
    }
}