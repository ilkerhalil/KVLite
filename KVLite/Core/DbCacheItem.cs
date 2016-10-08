// File name: DbCacheItem.cs
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT
// OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using LinqToDB.Mapping;
using System.Text;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Represents an item stored inside the cache.
    /// </summary>
    internal sealed class DbCacheItem
    {
        [Column(Name = "KVLI_ID"), PrimaryKey]
        public long Id { get; set; }

        [Column(Name = "KVLI_PARTITION"), NotNull]
        public string Partition { get; set; }

        [Column(Name = "KVLI_KEY"), NotNull]
        public string Key { get; set; }

        [Column(Name = "KVLI_VALUE"), NotNull]
        public byte[] Value { get; set; }

        [Column(Name = "KVLI_CREATION"), NotNull]
        public long UtcCreation { get; set; }

        [Column(Name = "KVLI_EXPIRY"), NotNull]
        public long UtcExpiry { get; set; }

        [Column(Name = "KVLI_INTERVAL"), NotNull]
        public long Interval { get; set; }

        [Column(Name = "KVLI_PARENT0")]
        public long? ParentId0 { get; set; }

        [Column(Name = "KVLI_PARENT1")]
        public long? ParentId1 { get; set; }

        [Column(Name = "KVLI_PARENT2")]
        public long? ParentId2 { get; set; }

        [Column(Name = "KVLI_PARENT3")]
        public long? ParentId3 { get; set; }

        [Column(Name = "KVLI_PARENT4")]
        public long? ParentId4 { get; set; }

        public static long Hash(string p, string k)
        {
            var ph = (long) XXHash.XXH32(Encoding.Default.GetBytes(p));
            var kh = (long) XXHash.XXH32(Encoding.Default.GetBytes(k));
            return (ph << 32) + kh;
        }
    }
}
