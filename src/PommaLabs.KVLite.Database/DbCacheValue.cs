// File name: DbCacheValue.cs
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
    ///   Represents a flat value stored inside the cache.
    /// </summary>
    public class DbCacheValue
    {
        /// <summary>
        ///   Database agnostic "true".
        /// </summary>
        public const byte True = 1;

        /// <summary>
        ///   Database agnostic "false".
        /// </summary>
        public const byte False = 0;

        /// <summary>
        ///   SQL column name of <see cref="Hash"/>.
        /// </summary>
        public const string HashColumn = "kvle_hash";

        /// <summary>
        ///   Hash of partition and key.
        /// </summary>
        public ulong Hash { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="Partition"/>.
        /// </summary>
        public const string PartitionColumn = "kvle_partition";

        /// <summary>
        ///   A partition holds a group of related keys.
        /// </summary>
        public string Partition { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="Key"/>.
        /// </summary>
        public const string KeyColumn = "kvle_key";

        /// <summary>
        ///   A key uniquely identifies an entry inside a partition.
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="UtcExpiry"/>.
        /// </summary>
        public const string UtcExpiryColumn = "kvle_expiry";

        /// <summary>
        ///   When the entry will expire, expressed as seconds after UNIX epoch.
        /// </summary>
        public long UtcExpiry { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="Interval"/>.
        /// </summary>
        public const string IntervalColumn = "kvle_interval";

        /// <summary>
        ///   How many seconds should be used to extend expiry time when the entry is retrieved.
        /// </summary>
        public long Interval { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="Value"/>.
        /// </summary>
        public const string ValueColumn = "kvle_value";

        /// <summary>
        ///   Serialized and optionally compressed content of this entry.
        /// </summary>
        public byte[] Value { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="Compressed"/>.
        /// </summary>
        public const string CompressedColumn = "kvle_compressed";

        /// <summary>
        ///   Whether the entry content was compressed or not.
        /// </summary>
        public byte Compressed { get; set; }

        /// <summary>
        ///   SQL column name of <see cref="UtcCreation"/>.
        /// </summary>
        public const string UtcCreationColumn = "kvle_creation";

        /// <summary>
        ///   When the entry was created, expressed as seconds after UNIX epoch.
        /// </summary>
        public long UtcCreation { get; set; }

        /// <summary>
        ///   Serves as the default hash function.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public sealed override int GetHashCode()
        {
            var hash = 797;
            unchecked
            {
                const int bigPrime = 179426549;

                hash = bigPrime * hash + (int) (UtcCreation ^ (UtcCreation >> 32));
                hash = bigPrime * hash + Partition.GetHashCode();
                hash = bigPrime * hash + Key.GetHashCode();
            }
            return hash;
        }
    }
}
