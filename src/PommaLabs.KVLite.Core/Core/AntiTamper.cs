// File name: AntiTamper.cs
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

using PommaLabs.KVLite.Resources;
using System;
using System.Buffers;
using System.IO;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Writes and validates anti-tamper hash codes.
    /// </summary>
    public static class AntiTamper
    {
        private static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;

        /// <summary>
        ///   Writes an anti-tamper hash code into given stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void WriteAntiTamperHashCode<T>(Stream stream, T value)
            where T : class, IObjectWithHashCode64
        {
            var bytes = BitConverter.GetBytes(value.GetHashCode64());
            stream.Write(bytes, 0, sizeof(long));
        }

        /// <summary>
        ///   Reads and checks the anti-tamper hash code from given stream.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="stream">The stream.</param>
        /// <param name="value">The value.</param>
        public static void ReadAntiTamperHashCode<T>(Stream stream, T value)
            where T : class, IObjectWithHashCode64
        {
            long antiTamper;
            try
            {
                var bytes = ByteArrayPool.Rent(sizeof(long));
                stream.Read(bytes, 0, sizeof(long));
                antiTamper = BitConverter.ToInt64(bytes, 0);
            }
            catch (Exception ex)
            {
                // Probably, the hash was missing at all.
                throw new InvalidDataException(ErrorMessages.HashNotFound, ex);
            }

            // Value is valid if hashes match.
            if (antiTamper != value.GetHashCode64())
            {
                throw new InvalidDataException(string.Format(ErrorMessages.HashMismatch, value.GetHashCode64(), antiTamper));
            }
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
