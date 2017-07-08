// File name: DbCacheEntry.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2017 Alessio Parma <alessio.parma@gmail.com>
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

namespace PommaLabs.KVLite.Database
{
    /// <summary>
    ///   Represents a flat entry stored inside the cache.
    /// </summary>
    public sealed class DbCacheEntry : DbCacheValue
    {
        /// <summary>
        ///   SQL column name of <see cref="ParentHash0"/>.
        /// </summary>
        public const string ParentHash0Column = "kvle_parent_hash0";

        /// <summary>
        ///   Optional parent entry hash, used to link entries in a hierarchical way.
        /// </summary>
        public ulong ParentHash0 { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="ParentKey0"/>.
        /// </summary>
        public const string ParentKey0Column = "kvle_parent_key0";

        /// <summary>
        ///   Optional parent entry key, used to link entries in a hierarchical way.
        /// </summary>
        public string ParentKey0 { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="ParentHash1"/>.
        /// </summary>
        public const string ParentHash1Column = "kvle_parent_hash1";

        /// <summary>
        ///   Optional parent entry hash, used to link entries in a hierarchical way.
        /// </summary>
        public ulong ParentHash1 { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="ParentKey1"/>.
        /// </summary>
        public const string ParentKey1Column = "kvle_parent_key1";

        /// <summary>
        ///   Optional parent entry key, used to link entries in a hierarchical way.
        /// </summary>
        public string ParentKey1 { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="ParentHash2"/>.
        /// </summary>
        public const string ParentHash2Column = "kvle_parent_hash2";

        /// <summary>
        ///   Optional parent entry hash, used to link entries in a hierarchical way.
        /// </summary>
        public ulong ParentHash2 { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="ParentKey2"/>.
        /// </summary>
        public const string ParentKey2Column = "kvle_parent_key2";

        /// <summary>
        ///   Optional parent entry key, used to link entries in a hierarchical way.
        /// </summary>
        public string ParentKey2 { get; set; }

        /// <summary>
        ///   Used to query a group of entries.
        /// </summary>
        public sealed class Group
        {
            /// <summary>
            ///   Hash of partition and key.
            /// </summary>
            public long? Hash { get; set; }

            /// <summary>
            ///   A partition holds a group of related keys.
            /// </summary>
            public string Partition { get; set; }

            /// <summary>
            ///   Retrieve an entry even if it has expired.
            /// </summary>
            public byte IgnoreExpiryDate { get; set; }

            /// <summary>
            ///   When the entry will expire, expressed as seconds after UNIX epoch.
            /// </summary>
            public long UtcExpiry { get; set; }
        }

        /// <summary>
        ///   Used to query a single entry.
        /// </summary>
        public sealed class Single
        {
            /// <summary>
            ///   Hash of partition and key.
            /// </summary>
            public ulong Hash { get; set; }

            /// <summary>
            ///   A partition holds a group of related keys.
            /// </summary>
            public string Partition { get; set; }

            /// <summary>
            ///   A key uniquely identifies an entry inside a partition.
            /// </summary>
            public string Key { get; set; }

            /// <summary>
            ///   Retrieve an entry even if it has expired.
            /// </summary>
            public byte IgnoreExpiryDate { get; set; }

            /// <summary>
            ///   When the entry will expire, expressed as seconds after UNIX epoch.
            /// </summary>
            public long UtcExpiry { get; set; }
        }
    }
}
