// File name: PooledMemoryStream.cs
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

using System;
using System.Buffers;
using System.IO;

namespace PommaLabs.KVLite.Core
{
    /// <summary>
    ///   Custom memory stream which relies on <see cref="ArrayPool{T}"/> for byte array management.
    /// </summary>
    public sealed class PooledMemoryStream : Stream
    {
        /// <summary>
        ///   Pool used to retrieve byte arrays.
        /// </summary>
        private static readonly ArrayPool<byte> ByteArrayPool = ArrayPool<byte>.Shared;

        /// <summary>
        ///   Buffer used by the stream.
        /// </summary>
        private byte[] _buffer;

        /// <summary>
        ///   Whether current buffer has been retrieved from the pool or not.
        /// </summary>
        private bool _pooled;

        /// <summary>
        ///   Stream length.
        /// </summary>
        private int _length;

        /// <summary>
        ///   Stream position.
        /// </summary>
        private int _position;

        /// <summary>
        ///   Whether current stream has been disposed or not.
        /// </summary>
        private bool _disposed;

        /// <summary>
        ///   Builds a memory stream with a buffer of 1024 bytes.
        /// </summary>
        public PooledMemoryStream()
        {
            _buffer = ByteArrayPool.Rent(1024);
            _pooled = true;
            _length = 0;
        }

        /// <summary>
        ///   Builds a memory stream with specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        public PooledMemoryStream(byte[] buffer)
        {
            _buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
            _pooled = false;
            _length = buffer.Length;
        }

        /// <summary>
        ///   When overridden in a derived class, gets a value indicating whether the current stream
        ///   supports reading.
        /// </summary>
        /// <returns>true if the stream supports reading; otherwise, false.</returns>
        public override bool CanRead => !_disposed ? true : throw new ObjectDisposedException(nameof(PooledMemoryStream));

        /// <summary>
        ///   When overridden in a derived class, gets a value indicating whether the current stream
        ///   supports seeking.
        /// </summary>
        /// <returns>true if the stream supports seeking; otherwise, false.</returns>
        public override bool CanSeek => !_disposed ? true : throw new ObjectDisposedException(nameof(PooledMemoryStream));

        /// <summary>
        ///   When overridden in a derived class, gets a value indicating whether the current stream
        ///   supports writing.
        /// </summary>
        /// <returns>true if the stream supports writing; otherwise, false.</returns>
        public override bool CanWrite => !_disposed ? true : throw new ObjectDisposedException(nameof(PooledMemoryStream));

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
        public override long Length => !_disposed ? _length : throw new ObjectDisposedException(nameof(PooledMemoryStream));

        /// <summary>
        ///   When overridden in a derived class, gets or sets the position within the current stream.
        /// </summary>
        /// <returns>The current position within the stream.</returns>
        /// <exception cref="NotSupportedException">The stream does not support seeking.</exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override long Position
        {
            get => !_disposed ? _position : throw new ObjectDisposedException(nameof(PooledMemoryStream));
            set => Seek(value, SeekOrigin.Begin);
        }

        /// <summary>
        ///   When overridden in a derived class, clears all buffers for this stream and causes any
        ///   buffered data to be written to the underlying device.
        /// </summary>
        public override void Flush()
        {
            // Nothing to do.
        }

        /// <summary>
        ///   When overridden in a derived class, reads a sequence of bytes from the current stream
        ///   and advances the position within the stream by the number of bytes read.
        /// </summary>
        /// <returns>
        ///   The total number of bytes read into the buffer. This can be less than the number of
        ///   bytes requested if that many bytes are not currently available, or zero (0) if the end
        ///   of the stream has been reached.
        /// </returns>
        /// <param name="buffer">
        ///   An array of bytes. When this method returns, the buffer contains the specified byte
        ///   array with the values between <paramref name="offset"/> and ( <paramref name="offset"/>
        ///   + <paramref name="count"/> - 1) replaced by the bytes read from the current source.
        /// </param>
        /// <param name="offset">
        ///   The zero-based byte offset in <paramref name="buffer"/> at which to begin storing the
        ///   data read from the current stream.
        /// </param>
        /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
        /// <exception cref="ArgumentException">
        ///   The sum of <paramref name="offset"/> and <paramref name="count"/> is larger than the
        ///   buffer length.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="offset"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override int Read(byte[] buffer, int offset, int count)
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset + count > buffer.Length) throw new ArgumentException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();

            // Read no more bytes than what we currently have.
            count = Math.Min(count, _length - _position);

            Buffer.BlockCopy(_buffer, _position, buffer, offset, count);

            _position += count;
            return count;
        }

        /// <summary>
        ///   Reads a byte from the stream and advances the position within the stream by one byte,
        ///   or returns -1 if at the end of the stream.
        /// </summary>
        /// <returns>
        ///   The unsigned byte cast to an <see cref="int"/>, or -1 if at the end of the stream.
        /// </returns>
        /// <exception cref="NotSupportedException">The stream does not support reading.</exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override int ReadByte()
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));

            if (_position == _length)
            {
                // End of stream.
                return -1;
            }
            return _buffer[_position++];
        }

        /// <summary>
        ///   When overridden in a derived class, sets the position within the current stream.
        /// </summary>
        /// <returns>The new position within the current stream.</returns>
        /// <param name="offset">A byte offset relative to the <paramref name="origin"/> parameter.</param>
        /// <param name="origin">
        ///   A value of type <see cref="SeekOrigin"/> indicating the reference point used to obtain
        ///   the new position.
        /// </param>
        /// <exception cref="NotSupportedException">
        ///   The stream does not support seeking, such as if the stream is constructed from a pipe
        ///   or console output.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override long Seek(long offset, SeekOrigin origin)
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));

            switch (origin)
            {
                case SeekOrigin.Begin:
                    _position = Math.Min((int) offset, _length);
                    break;

                case SeekOrigin.Current:
                    _position = Math.Min(_position + (int) offset, _length);
                    break;

                case SeekOrigin.End:
                    _position = _length;
                    break;
            }
            return _position;
        }

        /// <summary>
        ///   When overridden in a derived class, sets the length of the current stream.
        /// </summary>
        /// <param name="value">The desired length of the current stream in bytes.</param>
        /// <exception cref="NotSupportedException">
        ///   The stream does not support both writing and seeking, such as if the stream is
        ///   constructed from a pipe or console output.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override void SetLength(long value) => throw new NotSupportedException();

        /// <summary>
        ///   Writes the stream contents to a byte array, regardless of the <see cref="Position"/> property.
        /// </summary>
        /// <returns>A new byte array.</returns>
        public byte[] ToArray()
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));

            var copy = new byte[_length];
            Buffer.BlockCopy(_buffer, 0, copy, 0, _length);
            return copy;
        }

        /// <summary>
        ///   When overridden in a derived class, writes a sequence of bytes to the current stream
        ///   and advances the current position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="buffer">
        ///   An array of bytes. This method copies <paramref name="count"/> bytes from
        ///   <paramref name="buffer"/> to the current stream.
        /// </param>
        /// <param name="offset">
        ///   The zero-based byte offset in <paramref name="buffer"/> at which to begin copying bytes
        ///   to the current stream.
        /// </param>
        /// <param name="count">The number of bytes to be written to the current stream.</param>
        /// <exception cref="ArgumentException">
        ///   The sum of  <paramref name="offset"/> and  <paramref name="count"/> is greater than the
        ///   buffer length.
        /// </exception>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="offset"/> or  <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="NotSupportedException">The stream does not support writing.</exception>
        /// <exception cref="ObjectDisposedException">
        ///   <see cref="Write(byte[],int,int)"/> was called after the stream was closed.
        /// </exception>
        public override void Write(byte[] buffer, int offset, int count)
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (offset + count > buffer.Length) throw new ArgumentException();
            if (offset < 0 || count < 0) throw new ArgumentOutOfRangeException();

            var newPosition = _position + count;
            if (newPosition > _buffer.Length)
            {
                // Current buffer is too small, we need to enlarge it before writing new bytes.
                EnlargeYourBuffer(newPosition);
            }

            Buffer.BlockCopy(buffer, offset, _buffer, _position, count);

            _position = newPosition;
            _length = Math.Max(_length, newPosition);
        }

        /// <summary>
        ///   Writes a byte to the current position in the stream and advances the position within
        ///   the stream by one byte.
        /// </summary>
        /// <param name="value">The byte to write to the stream.</param>
        /// <exception cref="NotSupportedException">
        ///   The stream does not support writing, or the stream is already closed.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   Methods were called after the stream was closed.
        /// </exception>
        public override void WriteByte(byte value)
        {
            // Preconditions
            if (_disposed) throw new ObjectDisposedException(nameof(PooledMemoryStream));

            if (_position == _buffer.Length)
            {
                // Current buffer is too small, we need to enlarge it before writing new bytes.
                EnlargeYourBuffer(_position + 1);
            }

            _buffer[_position++] = value;
            _length = Math.Max(_length, _position);
        }

        /// <summary>
        ///   Releases the unmanaged resources used by the <see cref="Stream"/> and optionally
        ///   releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        ///   true to release both managed and unmanaged resources; false to release only unmanaged resources.
        /// </param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && !_disposed && _pooled)
            {
                ByteArrayPool.Return(_buffer);
                _buffer = null;
                _pooled = false;
            }

            // Mark this object as disposed, in order to disallow its usage.
            _disposed = true;

            base.Dispose(disposing);
        }

        /// <summary>
        ///   Increases the length of stream buffer and returns old one to the pool.
        /// </summary>
        /// <param name="newRequiredLength">New required length.</param>
        private void EnlargeYourBuffer(int newRequiredLength)
        {
            // Compute the exponent which should be applied as a power of 2.
            var exp = (int) Math.Ceiling(Math.Log((double) newRequiredLength / _buffer.Length, 2.0));

            // Ask for a buffer whose length is 2^exp the previous one.
            var biggerBuffer = ByteArrayPool.Rent(_buffer.Length * (1 << exp));

            // Copy all required bytes from previous buffer.
            Buffer.BlockCopy(_buffer, 0, biggerBuffer, 0, _length);

            // Return previous buffer to the pool.
            if (_pooled)
            {
                ByteArrayPool.Return(_buffer);
            }

            // Replace stream buffer with new buffer.
            _buffer = biggerBuffer;
            _pooled = true;
        }
    }
}
