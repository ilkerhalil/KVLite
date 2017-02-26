// File name: NoOpCompressor.cs
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
    ///   A special compressor which does simply nothing. It might be used in tests.
    /// </summary>
    public sealed class NoOpCompressor : ICompressor
    {
        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static NoOpCompressor Instance { get; } = new NoOpCompressor();

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
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateCompressionStream(Stream backingStream) => new NoOpStream(backingStream);

#pragma warning restore CC0022 // Should dispose object

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
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateDecompressionStream(Stream backingStream) => new NoOpStream(backingStream);

#pragma warning restore CC0022 // Should dispose object

        /// <summary>
        ///   A NoOp stream which simply wraps given stream.
        /// </summary>
        public sealed class NoOpStream : Stream
        {
            private readonly Stream _inner;

            /// <summary>
            ///   Builds a NoOp stream which relies on given <paramref name="inner"/> stream.
            /// </summary>
            /// <param name="inner">The inner stream.</param>
            public NoOpStream(Stream inner)
            {
                _inner = inner;
            }

            /// <summary>
            ///   When overridden in a derived class, clears all buffers for this stream and causes
            ///   any buffered data to be written to the underlying device.
            /// </summary>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            public override void Flush()
            {
                _inner.Flush();
            }

            /// <summary>
            ///   When overridden in a derived class, sets the position within the current stream.
            /// </summary>
            /// <returns>The new position within the current stream.</returns>
            /// <param name="offset">
            ///   A byte offset relative to the <paramref name="origin"/> parameter.
            /// </param>
            /// <param name="origin">
            ///   A value of type <see cref="SeekOrigin"/> indicating the reference point used to
            ///   obtain the new position.
            /// </param>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            /// <exception cref="NotSupportedException">
            ///   The stream does not support seeking, such as if the stream is constructed from a
            ///   pipe or console output.
            /// </exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override long Seek(long offset, SeekOrigin origin) => _inner.Seek(offset, origin);

            /// <summary>
            ///   When overridden in a derived class, sets the length of the current stream.
            /// </summary>
            /// <param name="value">The desired length of the current stream in bytes.</param>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            /// <exception cref="NotSupportedException">
            ///   The stream does not support both writing and seeking, such as if the stream is
            ///   constructed from a pipe or console output.
            /// </exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override void SetLength(long value)
            {
                _inner.SetLength(value);
            }

            /// <summary>
            ///   When overridden in a derived class, reads a sequence of bytes from the current
            ///   stream and advances the position within the stream by the number of bytes read.
            /// </summary>
            /// <returns>
            ///   The total number of bytes read into the buffer. This can be less than the number of
            ///   bytes requested if that many bytes are not currently available, or zero (0) if the
            ///   end of the stream has been reached.
            /// </returns>
            /// <param name="buffer">
            ///   An array of bytes. When this method returns, the buffer contains the specified byte
            ///   array with the values between <paramref name="offset"/> and (
            ///   <paramref name="offset"/> + <paramref name="count"/> - 1) replaced by the bytes
            ///   read from the current source.
            /// </param>
            /// <param name="offset">
            ///   The zero-based byte offset in <paramref name="buffer"/> at which to begin storing
            ///   the data read from the current stream.
            /// </param>
            /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
            /// <exception cref="ArgumentException">
            ///   The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than
            ///   the buffer length.
            /// </exception>
            /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">
            ///   <paramref name="offset"/> or <paramref name="count"/> is negative.
            /// </exception>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);

            /// <summary>
            ///   When overridden in a derived class, writes a sequence of bytes to the current
            ///   stream and advances the current position within this stream by the number of bytes written.
            /// </summary>
            /// <param name="buffer">
            ///   An array of bytes. This method copies <paramref name="count"/> bytes from
            ///   <paramref name="buffer"/> to the current stream.
            /// </param>
            /// <param name="offset">
            ///   The zero-based byte offset in <paramref name="buffer"/> at which to begin copying
            ///   bytes to the current stream.
            /// </param>
            /// <param name="count">The number of bytes to be written to the current stream.</param>
            /// <exception cref="ArgumentException">
            ///   The sum of <paramref name="offset"/> and <paramref name="count"/> is greater than
            ///   the buffer length.
            /// </exception>
            /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
            /// <exception cref="ArgumentOutOfRangeException">
            ///   <paramref name="offset"/> or <paramref name="count"/> is negative.
            /// </exception>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override void Write(byte[] buffer, int offset, int count)
            {
                _inner.Write(buffer, offset, count);
            }

            /// <summary>
            ///   When overridden in a derived class, gets a value indicating whether the current
            ///   stream supports reading.
            /// </summary>
            /// <returns>true if the stream supports reading; otherwise, false.</returns>
            public override bool CanRead => _inner.CanRead;

            /// <summary>
            ///   When overridden in a derived class, gets a value indicating whether the current
            ///   stream supports seeking.
            /// </summary>
            /// <returns>true if the stream supports seeking; otherwise, false.</returns>
            public override bool CanSeek => _inner.CanSeek;

            /// <summary>
            ///   When overridden in a derived class, gets a value indicating whether the current
            ///   stream supports writing.
            /// </summary>
            /// <returns>true if the stream supports writing; otherwise, false.</returns>
            public override bool CanWrite => _inner.CanWrite;

            /// <summary>
            ///   When overridden in a derived class, gets the length in bytes of the stream.
            /// </summary>
            /// <returns>A long value representing the length of the stream in bytes.</returns>
            /// <exception cref="NotSupportedException">
            ///   A class derived from Stream does not support seeking.
            /// </exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override long Length => _inner.Length;

            /// <summary>
            ///   When overridden in a derived class, gets or sets the position within the current stream.
            /// </summary>
            /// <returns>The current position within the stream.</returns>
            /// <exception cref="IOException">An I/O error occurs.</exception>
            /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
            /// <exception cref="ObjectDisposedException">
            ///   Methods were called after the stream was closed.
            /// </exception>
            public override long Position
            {
                get { return _inner.Position; }
                set { _inner.Position = value; }
            }
        }
    }
}
