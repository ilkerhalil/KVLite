// File name: AntiTamper.cs
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

using PommaLabs.KVLite.Resources;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Writes and validates anti-tamper hash codes.
    /// </summary>
    public static class AntiTamper
    {
        /// <summary>
        ///   Writes an anti-tamper hash code into given stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="s">The stream.</param>
        /// <param name="v">The value.</param>
        public static void WriteAntiTamperHashCode<T>(Stream s, T v)
            where T : class, IObjectWithHashCode64
        {
            var c = new LongToBytesConverter { Long = v.GetHashCode64() };

            s.WriteByte(c.Byte1);
            s.WriteByte(c.Byte2);
            s.WriteByte(c.Byte3);
            s.WriteByte(c.Byte4);
            s.WriteByte(c.Byte5);
            s.WriteByte(c.Byte6);
            s.WriteByte(c.Byte7);
            s.WriteByte(c.Byte8);
        }

        /// <summary>
        ///   Reads and checks the anti-tamper hash code from given stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="s">The stream.</param>
        /// <param name="v">The value.</param>
        public static void ReadAntiTamperHashCode<T>(Stream s, T v)
            where T : class, IObjectWithHashCode64
        {
            LongToBytesConverter c;
            try
            {
                c = new LongToBytesConverter
                {
                    Byte1 = (byte) s.ReadByte(),
                    Byte2 = (byte) s.ReadByte(),
                    Byte3 = (byte) s.ReadByte(),
                    Byte4 = (byte) s.ReadByte(),
                    Byte5 = (byte) s.ReadByte(),
                    Byte6 = (byte) s.ReadByte(),
                    Byte7 = (byte) s.ReadByte(),
                    Byte8 = (byte) s.ReadByte()
                };
            }
            catch (Exception ex)
            {
                // Probably, the hash was missing at all.
                throw new InvalidDataException(ErrorMessages.HashNotFound, ex);
            }

            // Value is valid if hashes match.
            if (c.Long != v.GetHashCode64())
            {
                throw new InvalidDataException(string.Format(ErrorMessages.HashMismatch, v.GetHashCode64(), c.Long));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct LongToBytesConverter
        {
            [FieldOffset(0)]
            public long Long;

            [FieldOffset(0)]
            public byte Byte1;

            [FieldOffset(1)]
            public byte Byte2;

            [FieldOffset(2)]
            public byte Byte3;

            [FieldOffset(3)]
            public byte Byte4;

            [FieldOffset(4)]
            public byte Byte5;

            [FieldOffset(5)]
            public byte Byte6;

            [FieldOffset(6)]
            public byte Byte7;

            [FieldOffset(7)]
            public byte Byte8;
        }

        /// <summary>
        ///   Represents an object with a 64 bit long hash.
        /// </summary>
        public interface IObjectWithHashCode64
        {
            /// <summary>
            ///   Returns a 64 bit long hash.
            /// </summary>
            /// <returns>A 64 bit long hash.</returns>
            long GetHashCode64();
        }
    }
}
