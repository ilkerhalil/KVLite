// File name: AntiTamper.cs
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

using System;
using System.IO;
using System.Runtime.InteropServices;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Writes and validates anti-tamper hash codes.
    /// </summary>
    internal static class AntiTamper
    {
        public static void WriteAntiTamperHashCode(Stream s, DbCacheValue v)
        {
            var c = new IntegerToBytesConverter { Integer = v.GetHashCode() };

            s.WriteByte(c.Byte1);
            s.WriteByte(c.Byte2);
            s.WriteByte(c.Byte3);
            s.WriteByte(c.Byte4);
        }

        public static void ReadAntiTamperHashCode(Stream s, DbCacheValue v)
        {
            IntegerToBytesConverter c;
            try
            {
                c = new IntegerToBytesConverter
                {
                    Byte1 = (byte) s.ReadByte(),
                    Byte2 = (byte) s.ReadByte(),
                    Byte3 = (byte) s.ReadByte(),
                    Byte4 = (byte) s.ReadByte()
                };
            }
            catch (Exception ex)
            {
                // Probably, the hash was missing at all.
                throw new InvalidDataException(ErrorMessages.HashNotFound, ex);
            }

            // Value is valid if hashes match.
            if (c.Integer != v.GetHashCode())
            {
                throw new InvalidDataException(string.Format(ErrorMessages.HashMismatch, v.GetHashCode(), c.Integer));
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct IntegerToBytesConverter
        {
            [FieldOffset(0)]
            public int Integer;

            [FieldOffset(0)]
            public byte Byte1;

            [FieldOffset(1)]
            public byte Byte2;

            [FieldOffset(2)]
            public byte Byte3;

            [FieldOffset(3)]
            public byte Byte4;
        }
    }
}
