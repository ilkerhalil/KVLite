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

using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PommaLabs.KVLite
{
    /// <summary>
    ///   Represents an item stored inside the cache.
    /// </summary>
    public class DbCacheItem
    {
        [Column("KVLI_HASH"), Key, DatabaseGenerated(DatabaseGeneratedOption.None)]
        public virtual long Hash { get; set; }

        [Column("KVLI_PARTITION")]
        public virtual string Partition { get; set; }

        [Column("KVLI_KEY")]
        public virtual string Key { get; set; }

        [Column("KVLI_CREATION")]
        public virtual long UtcCreation { get; set; }

        [Column("KVLI_EXPIRY"), ConcurrencyCheck]
        public virtual long UtcExpiry { get; set; }

        [Column("KVLI_INTERVAL")]
        public virtual long Interval { get; set; }

        [Column("KVLI_COMPRESSED")]
        public virtual bool Compressed { get; set; }

        [Column("KVLI_PARENT_HASH0")]
        public virtual long? ParentHash0 { get; set; }

        [Column("KVLI_PARENT_KEY0")]
        public virtual string ParentKey0 { get; set; }

        [Column("KVLI_PARENT_HASH1")]
        public virtual long? ParentHash1 { get; set; }

        [Column("KVLI_PARENT_KEY1")]
        public virtual string ParentKey1 { get; set; }

        [Column("KVLI_PARENT_HASH2")]
        public virtual long? ParentHash2 { get; set; }

        [Column("KVLI_PARENT_KEY2")]
        public virtual string ParentKey2 { get; set; }

        [Column("KVLI_PARENT_HASH3")]
        public virtual long? ParentHash3 { get; set; }

        [Column("KVLI_PARENT_KEY3")]
        public virtual string ParentKey3 { get; set; }

        [Column("KVLI_PARENT_HASH4")]
        public virtual long? ParentHash4 { get; set; }

        [Column("KVLI_PARENT_KEY4")]
        public virtual string ParentKey4 { get; set; }

        [Column("KVLI_VALUE")]
        public virtual byte[] Value { get; set; }
    }
}