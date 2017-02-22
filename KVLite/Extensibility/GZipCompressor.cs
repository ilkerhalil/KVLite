// Copyright 2015-2025 Alessio Parma <alessio.parma@gmail.com>
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
    ///   Compressor backed by <see cref="GZipStream"/>.
    /// </summary>
    public sealed class GZipCompressor : ICompressor
    {
#if !NET40

        /// <summary>
        ///   The compression level.
        /// </summary>
        private readonly CompressionLevel _compressionLevel;

        /// <summary>
        ///   Builds a GZip compressor using <see cref="CompressionLevel.Fastest"/> as the
        ///   compression level.
        /// </summary>
        public GZipCompressor()
            : this(CompressionLevel.Fastest)
        {
        }

        /// <summary>
        ///   Builds a GZip compressor using the specified level as the compression level.
        /// </summary>
        /// <param name="compressionLevel">The compression level.</param>
        public GZipCompressor(CompressionLevel compressionLevel)
        {
            // Preconditions
            Raise.ArgumentException.IfIsNotValidEnum(compressionLevel, nameof(compressionLevel));

            _compressionLevel = compressionLevel;
        }

#else

        /// <summary>
        ///   Builds a GZip compressor using an optimal compression level.
        /// </summary>
        public GZipCompressor()
        {
        }

#endif

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static GZipCompressor Instance { get; } = new GZipCompressor();

        /// <summary>
        ///   Creates a new compression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new compression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateCompressionStream(Stream backingStream)
        {
#if !NET40
            return new GZipStream(backingStream, _compressionLevel, true);
#else
            return new GZipStream(backingStream, CompressionMode.Compress, true);
#endif
        }

#pragma warning restore CC0022 // Should dispose object

        /// <summary>
        ///   Creates a new decompression stream.
        /// </summary>
        /// <param name="backingStream">The backing stream.</param>
        /// <returns>A new decompression stream.</returns>
#pragma warning disable CC0022 // Should dispose object

        public Stream CreateDecompressionStream(Stream backingStream) => new GZipStream(backingStream, CompressionMode.Decompress, true);

#pragma warning restore CC0022 // Should dispose object
    }
}
