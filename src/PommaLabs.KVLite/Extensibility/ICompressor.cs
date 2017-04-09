// File name: ICompressor.cs
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

using System;
using System.IO;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Represents a service whose only goal is compression and decompression. This kind of object
    ///   can compress streams or byte arrays.
    /// </summary>
    public interface ICompressor
    {
        /// <summary>
        ///   Creates a compression stream using given stream as backend.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>
        ///   A stream which can be used for compression. Returned stream is backed by the input stream.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="backingStream"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="backingStream"/> cannot be written.
        /// </exception>
        Stream CreateCompressionStream(Stream backingStream);

        /// <summary>
        ///   Creates a decompression stream using given stream as backend.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>
        ///   A stream which can be used for decompression. Returned stream is backed by the input stream.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="backingStream"/> is null.</exception>
        /// <exception cref="ArgumentException">
        ///   <paramref name="backingStream"/> cannot be written.
        /// </exception>
        Stream CreateDecompressionStream(Stream backingStream);
    }
}
