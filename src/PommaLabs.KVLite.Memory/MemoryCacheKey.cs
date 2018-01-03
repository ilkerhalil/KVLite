﻿// File name: MemoryCacheKey.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2018 Alessio Parma <alessio.parma@gmail.com>
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

using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite.Memory
{
    internal sealed class MemoryCacheKey : IEquatable<MemoryCacheKey>
    {
        public MemoryCacheKey(string partition, string key)
        {
            Partition = partition;
            Key = key;
        }

        public string Partition { get; }

        public string Key { get; }

        public static string Serialize(string partition, string key)
        {
            var partitionLength = partition.Length;
            return $"{partitionLength}${partition}${key}";
        }

        public static MemoryCacheKey Deserialize(string serialized)
        {
            var partitionLengthEnd = serialized.IndexOf('$');
            var partitionLengthPrefix = serialized.Substring(0, partitionLengthEnd);
            var partitionLength = int.Parse(partitionLengthPrefix);
            var partition = serialized.Substring(partitionLengthEnd + 1, partitionLength);
            var key = serialized.Substring(partitionLengthEnd + partitionLength + 2);
            return new MemoryCacheKey(partition, key);
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as MemoryCacheKey);
        }

        public bool Equals(MemoryCacheKey other)
        {
            return other != null &&
                   Partition == other.Partition &&
                   Key == other.Key;
        }

        public override int GetHashCode()
        {
            var hashCode = -2068116195;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Partition);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Key);
            return hashCode;
        }

        public static bool operator ==(MemoryCacheKey key1, MemoryCacheKey key2)
        {
            return EqualityComparer<MemoryCacheKey>.Default.Equals(key1, key2);
        }

        public static bool operator !=(MemoryCacheKey key1, MemoryCacheKey key2)
        {
            return !(key1 == key2);
        }
    }
}
