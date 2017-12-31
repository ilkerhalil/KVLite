// File name: LZ4Compressor.cs
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

using LZ4;
using PommaLabs.KVLite.Extensibility;
using System.IO;
using System.IO.Compression;

namespace PommaLabs.KVLite.Benchmarks.Compression
{
    internal sealed class LZ4Compressor : ICompressor
    {
        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static LZ4Compressor Instance { get; } = new LZ4Compressor();

        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateCompressionStream(Stream backingStream) => new LZ4Stream(backingStream, CompressionMode.Compress, LZ4StreamFlags.IsolateInnerStream);

#pragma warning restore CC0022 // Should dispose object

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateDecompressionStream(Stream backingStream) => new LZ4Stream(backingStream, CompressionMode.Decompress, LZ4StreamFlags.IsolateInnerStream);

#pragma warning restore CC0022 // Should dispose object
    }
}
