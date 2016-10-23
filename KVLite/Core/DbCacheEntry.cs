// File name: DbCacheEntry.cs
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

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Represents a flat entry stored inside the cache.
    /// </summary>
    public sealed class DbCacheEntry
    {
        public const string HashColumn = "kvli_hash";

        public long Hash { get; set; }

        public const string PartitionColumn = "kvli_partition";

        public string Partition { get; set; }

        public const string KeyColumn = "kvli_key";

        public string Key { get; set; }

        public const string UtcCreationColumn = "kvli_creation";

        public long UtcCreation { get; set; }

        public const string UtcExpiryColumn = "kvli_expiry";

        public long UtcExpiry { get; set; }

        public const string IntervalColumn = "kvli_interval";

        public long Interval { get; set; }

        public const string ParentHash0Column = "kvli_parent_hash0";

        public long? ParentHash0 { get; set; }

        public const string ParentKey0Column = "kvli_parent_key0";

        public string ParentKey0 { get; set; }

        public const string ParentHash1Column = "kvli_parent_hash1";

        public long? ParentHash1 { get; set; }

        public const string ParentKey1Column = "kvli_parent_key1";

        public string ParentKey1 { get; set; }

        public const string ParentHash2Column = "kvli_parent_hash2";

        public long? ParentHash2 { get; set; }

        public const string ParentKey2Column = "kvli_parent_key2";

        public string ParentKey2 { get; set; }

        public const string ParentHash3Column = "kvli_parent_hash3";

        public long? ParentHash3 { get; set; }

        public const string ParentKey3Column = "kvli_parent_key3";

        public string ParentKey3 { get; set; }

        public const string ParentHash4Column = "kvli_parent_hash4";

        public long? ParentHash4 { get; set; }

        public const string ParentKey4Column = "kvli_parent_key4";

        public string ParentKey4 { get; set; }

        public const string ValueColumn = "kvlv_value";

        public byte[] Value { get; set; }

        public const string CompressedColumn = "kvlv_compressed";

        public bool Compressed { get; set; }

        public sealed class Group
        {
            public string Partition { get; set; }

            public bool IgnoreExpiryDate { get; set; }

            public long UtcExpiry { get; set; }
        }

        public sealed class Single
        {
            public long Hash { get; set; }

            public bool IgnoreExpiryDate { get; set; }

            public long UtcExpiry { get; set; }
        }
    }
}