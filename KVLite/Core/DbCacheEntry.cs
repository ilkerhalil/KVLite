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

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Represents a flat entry stored inside the cache.
    /// </summary>
    internal sealed class DbCacheEntry : DbCacheValue
    {
        public const string PartitionColumn = "kvle_partition";

        public string Partition { get; set; }

        public const string KeyColumn = "kvle_key";

        public string Key { get; set; }

        public const string UtcCreationColumn = "kvle_creation";

        public long UtcCreation { get; set; }

        public const string ParentKey0Column = "kvle_parent_key0";

        public string ParentKey0 { get; set; }

        public const string ParentKey1Column = "kvle_parent_key1";

        public string ParentKey1 { get; set; }

        public const string ParentKey2Column = "kvle_parent_key2";

        public string ParentKey2 { get; set; }

        public const string ParentKey3Column = "kvle_parent_key3";

        public string ParentKey3 { get; set; }

        public const string ParentKey4Column = "kvle_parent_key4";

        public string ParentKey4 { get; set; }

        public sealed class Group
        {
            public string Partition { get; set; }

            public bool IgnoreExpiryDate { get; set; }

            public long UtcExpiry { get; set; }
        }

        public sealed class Single
        {
            public string Partition { get; set; }

            public string Key { get; set; }

            public bool IgnoreExpiryDate { get; set; }

            public long UtcExpiry { get; set; }
        }
    }
}