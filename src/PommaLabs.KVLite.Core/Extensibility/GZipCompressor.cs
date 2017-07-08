// File name: GZipCompressor.cs
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
using System.IO.Compression;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Compressor backed by <see cref="GZipStream"/>.
    /// </summary>
    public sealed class GZipCompressor : ICompressor
    {
        /// <summary>
        ///   The compression level.
        /// </summary>
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        ///   Builds a GZip compressor using <see cref="DefaultCompressionLevel"/> as the compression level.
        /// </summary>
        public GZipCompressor()
            : this(DefaultCompressionLevel)
        {
        }

        /// <summary>
        ///   Builds a GZip compressor using the specified level as the compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        public GZipCompressor(CompressionLevel compressionLevel)
        {
            // Preconditions
            if (!Enum.IsDefined(typeof(CompressionLevel), compressionLevel)) throw new ArgumentException(ErrorMessages.InvalidCompressionLevel, nameof(compressionLevel));

            _compressionLevel = compressionLevel;
        }

        /// <summary>
        ///   The default compression level, used when none has been specified. It is equal to
        ///   <see cref="CompressionLevel.Fastest"/>, since we do not want to spend a lot of time
        ///   compressing and decompressing data.
        /// </summary>
        public static CompressionLevel DefaultCompressionLevel { get; } = CompressionLevel.Fastest;

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static GZipCompressor Instance { get; } = new GZipCompressor();

        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
        public Stream CreateCompressionStream(Stream backingStream) => new GZipStream(backingStream, _compressionLevel, true);

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
        public Stream CreateDecompressionStream(Stream backingStream) => new GZipStream(backingStream, CompressionMode.Decompress, true);
    }
}
