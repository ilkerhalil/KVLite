﻿// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
//
// Licensed under the Apache License, Version 2.0 (the "License"); you may not use this file except
// in compliance with the License. You may obtain a copy of the License at:
//
// "http://www.apache.org/licenses/LICENSE-2.0"
//
// Unless required by applicable law or agreed to in writing, software distributed under the License
// is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
// or implied. See the License for the specific language governing permissions and limitations under
// the License.

using PommaLabs.Thrower;
using System.IO;
using System.IO.Compression;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   Compressor backed by <see cref="DeflateStream"/>.
    /// </summary>
    public sealed class DeflateCompressor : ICompressor
    {
        /// <summary>
        ///   The compression level.
        /// </summary>
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        ///   Builds a Deflate compressor using <see cref="DefaultCompressionLevel"/> as the
        ///   compression level.
        /// </summary>
        public DeflateCompressor()
            : this(DefaultCompressionLevel)
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
        ///   The default compression level, used when none has been specified. It is equal to
        ///   <see cref="CompressionLevel.Fastest"/>, since we do not want to spend a lot of time
        ///   compressing and decompressing data.
        /// </summary>
        public static CompressionLevel DefaultCompressionLevel { get; } = CompressionLevel.Fastest;

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static DeflateCompressor Instance { get; } = new DeflateCompressor();

        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
        public Stream CreateCompressionStream(Stream backingStream) => new DeflateStream(backingStream, _compressionLevel, true);

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
        public Stream CreateDecompressionStream(Stream backingStream) => new DeflateStream(backingStream, CompressionMode.Decompress, true);
    }
}
