// File name: DeflateCompressor.cs
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

using PommaLabs.KVLite.Extensibility;
using PommaLabs.Thrower;
using System.IO;
using System.IO.Compression;

namespace PommaLabs.KVLite.Benchmarks.Compressors
{
    internal sealed class DeflateCompressor : ICompressor
    {
        /// <summary>
        ///   The compression level.
        /// </summary>
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        ///   Builds a Deflate compressor using <see cref="CompressionLevel.Fastest"/> as the
        ///   compression level.
        /// </summary>
        public DeflateCompressor()
            : this(CompressionLevel.Fastest)
        {
        }

        /// <summary>
        ///   Builds a Deflate compressor using the specified level as the compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        public DeflateCompressor(CompressionLevel compressionLevel)
        {
            // Preconditions
            Raise.ArgumentException.IfIsNotValidEnum(compressionLevel, nameof(compressionLevel));

            _compressionLevel = compressionLevel;
        }

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static DeflateCompressor Instance { get; } = new DeflateCompressor();

        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateCompressionStream(Stream backingStream)
        {
            return new DeflateStream(backingStream, _compressionLevel, true);
        }

#pragma warning restore CC0022 // Should dispose object

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateDecompressionStream(Stream backingStream) => new DeflateStream(backingStream, CompressionMode.Decompress, true);

#pragma warning restore CC0022 // Should dispose object
    }
}
