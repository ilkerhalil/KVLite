// File name: SnappyCompressor.cs
// 
// Author(s): Alessio Parma <alessio.parma@gmail.com>
// 
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
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
// DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using System.IO;
using System.IO.Compression;
using Finsa.CodeServices.Compression;
using PommaLabs.KVLite.Core.Snappy;

namespace PommaLabs.KVLite.CodeServices.Compression
{
    /// <summary>
    ///   A custom compressor based on Snappy.
    /// </summary>
    public sealed class SnappyCompressor : AbstractCompressor
    {
        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
        protected override Stream CreateCompressionStreamInternal(Stream backingStream)
        {
            return new SnappyStream(backingStream, CompressionMode.Compress, true);
        }

        /// <summary>
        ///   Compresses the first stream into the second steam.
        /// </summary>
        /// <param name="decompressedStream">The decompressed stream.</param>
        /// <param name="compressedStream">The compressed stream.</param>
        protected override void CompressInternal(Stream decompressedStream, Stream compressedStream)
        {
            using (var snappyStream = CreateCompressionStreamInternal(compressedStream))
            {
                decompressedStream.CopyTo(snappyStream);
            }
        }

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
        protected override Stream CreateDecompressionStreamInternal(Stream backingStream)
        {
            return new SnappyStream(backingStream, CompressionMode.Decompress, true);
        }

        /// <summary>
        ///   Decompresses the first stream into the second steam.
        /// </summary>
        /// <param name="compressedStream">The compressed stream.</param>
        /// <param name="decompressedStream">The decompressed stream.</param>
        protected override void DecompressInternal(Stream compressedStream, Stream decompressedStream)
        {
            using (var snappyStream = CreateDecompressionStreamInternal(compressedStream))
            {
                snappyStream.CopyTo(decompressedStream);
            }
        }
    }
}
