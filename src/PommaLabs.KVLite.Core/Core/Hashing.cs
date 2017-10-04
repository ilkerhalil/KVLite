﻿// File name: Hashing.cs
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

using System.Runtime.CompilerServices;
using System.Text;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Module dedicated to fast hashing of strings (partion and key).
    /// </summary>
    public unsafe static class Hashing
    {
        /// <summary>
        ///   Hashes given partition; returns null if partition is null.
        /// </summary>
        /// <param name="partition">Partition.</param>
        /// <returns>Long hash for given partition, null if partition is null.</returns>
        public static long? HashPartition(string partition)
        {
            if (partition == null) return new long?();
            unchecked
            {
                return (long) XXHash32.CalculateRaw(partition) << 32;
            }
        }

        /// <summary>
        ///   Hashes given partition and key.
        /// </summary>
        /// <param name="partition">Partition.</param>
        /// <param name="key">Key.</param>
        /// <returns>Long hash for given partition and key.</returns>
        public static long HashPartitionAndKey(string partition, string key)
        {
            unchecked
            {
                var pHash = (long) XXHash32.CalculateRaw(partition) << 32;
                return pHash + XXHash32.CalculateRaw(key);
            }
        }

        /// <summary>
        ///   A port of the original XXHash algorithm from Google in 32bits.
        /// </summary>
        /// <remarks>
        ///   The 32bits and 64bits hashes for the same data are different. In short those are 2
        ///   entirely different algorithms.
        /// </remarks>
        internal static class XXHash32
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static unsafe uint CalculateInline(byte* buffer, int len, uint seed = 0)
            {
                unchecked
                {
                    uint h32;

                    byte* bEnd = buffer + len;

                    if (len >= 16)
                    {
                        byte* limit = bEnd - 16;

                        uint v1 = seed + PRIME32_1 + PRIME32_2;
                        uint v2 = seed + PRIME32_2;
                        uint v3 = seed + 0;
                        uint v4 = seed - PRIME32_1;

                        do
                        {
                            v1 += *((uint*) buffer) * PRIME32_2;
                            buffer += sizeof(uint);
                            v2 += *((uint*) buffer) * PRIME32_2;
                            buffer += sizeof(uint);
                            v3 += *((uint*) buffer) * PRIME32_2;
                            buffer += sizeof(uint);
                            v4 += *((uint*) buffer) * PRIME32_2;
                            buffer += sizeof(uint);

                            v1 = RotateLeft32(v1, 13);
                            v2 = RotateLeft32(v2, 13);
                            v3 = RotateLeft32(v3, 13);
                            v4 = RotateLeft32(v4, 13);

                            v1 *= PRIME32_1;
                            v2 *= PRIME32_1;
                            v3 *= PRIME32_1;
                            v4 *= PRIME32_1;
                        }
                        while (buffer <= limit);

                        h32 = RotateLeft32(v1, 1) + RotateLeft32(v2, 7) + RotateLeft32(v3, 12) + RotateLeft32(v4, 18);
                    }
                    else
                    {
                        h32 = seed + PRIME32_5;
                    }

                    h32 += (uint) len;

                    while (buffer + 4 <= bEnd)
                    {
                        h32 += *((uint*) buffer) * PRIME32_3;
                        h32 = RotateLeft32(h32, 17) * PRIME32_4;
                        buffer += 4;
                    }

                    while (buffer < bEnd)
                    {
                        h32 += (uint) (*buffer) * PRIME32_5;
                        h32 = RotateLeft32(h32, 11) * PRIME32_1;
                        buffer++;
                    }

                    h32 ^= h32 >> 15;
                    h32 *= PRIME32_2;
                    h32 ^= h32 >> 13;
                    h32 *= PRIME32_3;
                    h32 ^= h32 >> 16;

                    return h32;
                }
            }

            public static unsafe uint Calculate(byte* buffer, int len, uint seed = 0)
            {
                return CalculateInline(buffer, len, seed);
            }

            public static uint Calculate(string value, Encoding encoder, uint seed = 0)
            {
                var buf = encoder.GetBytes(value);

                fixed (byte* buffer = buf)
                {
                    return CalculateInline(buffer, buf.Length, seed);
                }
            }

            public static uint CalculateRaw(string buf, uint seed = 0)
            {
                fixed (char* buffer = buf)
                {
                    return CalculateInline((byte*) buffer, buf.Length * sizeof(char), seed);
                }
            }

            public static uint Calculate(byte[] buf, int len = -1, uint seed = 0)
            {
                if (len == -1)
                    len = buf.Length;

                fixed (byte* buffer = buf)
                {
                    return CalculateInline(buffer, len, seed);
                }
            }

            public static uint Calculate(int[] buf, int len = -1, uint seed = 0)
            {
                if (len == -1)
                    len = buf.Length;

                fixed (int* buffer = buf)
                {
                    return Calculate((byte*) buffer, len * sizeof(int), seed);
                }
            }

            private const uint PRIME32_1 = 2654435761U;
            private const uint PRIME32_2 = 2246822519U;
            private const uint PRIME32_3 = 3266489917U;
            private const uint PRIME32_4 = 668265263U;
            private const uint PRIME32_5 = 374761393U;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static uint RotateLeft32(uint value, int count)
            {
                return (value << count) | (value >> (32 - count));
            }
        }

        /// <summary>
        ///   A port of the original XXHash algorithm from Google in 64bits.
        /// </summary>
        /// <remarks>
        ///   The 32bits and 64bits hashes for the same data are different. In short those are 2
        ///   entirely different algorithms.
        /// </remarks>
        internal static class XXHash64
        {
            public static unsafe ulong Calculate(byte* buffer, int len, ulong seed = 0)
            {
                ulong h64;

                byte* bEnd = buffer + len;

                if (len >= 32)
                {
                    byte* limit = bEnd - 32;

                    ulong v1 = seed + PRIME64_1 + PRIME64_2;
                    ulong v2 = seed + PRIME64_2;
                    ulong v3 = seed + 0;
                    ulong v4 = seed - PRIME64_1;

                    do
                    {
                        v1 += *((ulong*) buffer) * PRIME64_2;
                        buffer += sizeof(ulong);
                        v2 += *((ulong*) buffer) * PRIME64_2;
                        buffer += sizeof(ulong);
                        v3 += *((ulong*) buffer) * PRIME64_2;
                        buffer += sizeof(ulong);
                        v4 += *((ulong*) buffer) * PRIME64_2;
                        buffer += sizeof(ulong);

                        v1 = RotateLeft64(v1, 31);
                        v2 = RotateLeft64(v2, 31);
                        v3 = RotateLeft64(v3, 31);
                        v4 = RotateLeft64(v4, 31);

                        v1 *= PRIME64_1;
                        v2 *= PRIME64_1;
                        v3 *= PRIME64_1;
                        v4 *= PRIME64_1;
                    }
                    while (buffer <= limit);

                    h64 = RotateLeft64(v1, 1) + RotateLeft64(v2, 7) + RotateLeft64(v3, 12) + RotateLeft64(v4, 18);

                    v1 *= PRIME64_2;
                    v1 = RotateLeft64(v1, 31);
                    v1 *= PRIME64_1;
                    h64 ^= v1;
                    h64 = h64 * PRIME64_1 + PRIME64_4;

                    v2 *= PRIME64_2;
                    v2 = RotateLeft64(v2, 31);
                    v2 *= PRIME64_1;
                    h64 ^= v2;
                    h64 = h64 * PRIME64_1 + PRIME64_4;

                    v3 *= PRIME64_2;
                    v3 = RotateLeft64(v3, 31);
                    v3 *= PRIME64_1;
                    h64 ^= v3;
                    h64 = h64 * PRIME64_1 + PRIME64_4;

                    v4 *= PRIME64_2;
                    v4 = RotateLeft64(v4, 31);
                    v4 *= PRIME64_1;
                    h64 ^= v4;
                    h64 = h64 * PRIME64_1 + PRIME64_4;
                }
                else
                {
                    h64 = seed + PRIME64_5;
                }

                h64 += (ulong) len;

                while (buffer + 8 <= bEnd)
                {
                    ulong k1 = *((ulong*) buffer);
                    k1 *= PRIME64_2;
                    k1 = RotateLeft64(k1, 31);
                    k1 *= PRIME64_1;
                    h64 ^= k1;
                    h64 = RotateLeft64(h64, 27) * PRIME64_1 + PRIME64_4;
                    buffer += 8;
                }

                if (buffer + 4 <= bEnd)
                {
                    h64 ^= *(uint*) buffer * PRIME64_1;
                    h64 = RotateLeft64(h64, 23) * PRIME64_2 + PRIME64_3;
                    buffer += 4;
                }

                while (buffer < bEnd)
                {
                    h64 ^= ((ulong) *buffer) * PRIME64_5;
                    h64 = RotateLeft64(h64, 11) * PRIME64_1;
                    buffer++;
                }

                h64 ^= h64 >> 33;
                h64 *= PRIME64_2;
                h64 ^= h64 >> 29;
                h64 *= PRIME64_3;
                h64 ^= h64 >> 32;

                return h64;
            }

            public static ulong Calculate(string value, Encoding encoder, ulong seed = 0)
            {
                var buf = encoder.GetBytes(value);

                fixed (byte* buffer = buf)
                {
                    return Calculate(buffer, buf.Length, seed);
                }
            }

            public static ulong CalculateRaw(string buf, ulong seed = 0)
            {
                fixed (char* buffer = buf)
                {
                    return Calculate((byte*) buffer, buf.Length * sizeof(char), seed);
                }
            }

            public static ulong Calculate(byte[] buf, int len = -1, ulong seed = 0)
            {
                if (len == -1)
                    len = buf.Length;

                fixed (byte* buffer = buf)
                {
                    return Calculate(buffer, len, seed);
                }
            }

            public static ulong Calculate(int[] buf, int len = -1, ulong seed = 0)
            {
                if (len == -1)
                    len = buf.Length;

                fixed (int* buffer = buf)
                {
                    return Calculate((byte*) buffer, len * sizeof(int), seed);
                }
            }

            private const ulong PRIME64_1 = 11400714785074694791UL;
            private const ulong PRIME64_2 = 14029467366897019727UL;
            private const ulong PRIME64_3 = 1609587929392839161UL;
            private const ulong PRIME64_4 = 9650029242287828579UL;
            private const ulong PRIME64_5 = 2870177450012600261UL;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            private static ulong RotateLeft64(ulong value, int count)
            {
                return (value << count) | (value >> (64 - count));
            }
        }
    }
}
