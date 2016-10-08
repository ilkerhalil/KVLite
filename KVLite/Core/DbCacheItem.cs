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
        [Column(Name = "KVLI_PARTITION"), PrimaryKey(Order = 0)]
        public string Partition { get; set; }

        [Column(Name = "KVLI_KEY"), PrimaryKey(Order = 1)]
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
        public string ParentKey0 { get; set; }

        [Column(Name = "KVLI_PARENT1")]
        public string ParentKey1 { get; set; }

        [Column(Name = "KVLI_PARENT2")]
        public string ParentKey2 { get; set; }

        [Column(Name = "KVLI_PARENT3")]
        public string ParentKey3 { get; set; }

        [Column(Name = "KVLI_PARENT4")]
        public string ParentKey4 { get; set; }
    }
}
