// File name: LogMessagesCompression.cs
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

using BenchmarkDotNet.Analysers;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Jobs;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;
using System.IO;

namespace PommaLabs.KVLite.Benchmarks.Compression
{
    [Config(typeof(Config))]
    public class LogMessagesCompression
    {
        private readonly ISerializer JsonSerializer = new JsonSerializer();
        private readonly ICompressor LZ4Compressor_Default = new LZ4Compressor();
        private readonly ICompressor GZipCompressor_Default = new GZipCompressor();
        private readonly ICompressor GZipCompressor_BestSpeed = new GZipCompressor(System.IO.Compression.CompressionLevel.Fastest);
        private readonly ICompressor DeflateCompressor_Default = new DeflateCompressor();
        private readonly ICompressor DeflateCompressor_BestSpeed = new DeflateCompressor(System.IO.Compression.CompressionLevel.Fastest);

        private class Config : ManualConfig
        {
            public Config()
            {
                Add(Job.Default);
                Add(CsvExporter.Default, HtmlExporter.Default, MarkdownExporter.GitHub, PlainExporter.Default);
                Add(new MemoryDiagnoser());
                Add(EnvironmentAnalyser.Default);
            }
        }

        [Params(10, 100, 1000)]
        public int Count { get; set; }

        [Benchmark]
        public long LZ4_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                using (var compressedStream = new PooledMemoryStream())
                using (var compressionStream = LZ4Compressor_Default.CreateCompressionStream(compressedStream))
                {
                    serializedStream.Position = 0L;
                    serializedStream.CopyTo(compressionStream);
                    return compressedStream.Length;
                }
            }
        }

        [Benchmark]
        public long GZip_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                using (var compressedStream = new PooledMemoryStream())
                using (var compressionStream = GZipCompressor_Default.CreateCompressionStream(compressedStream))
                {
                    serializedStream.Position = 0L;
                    serializedStream.CopyTo(compressionStream);
                    return compressedStream.Length;
                }
            }
        }

        [Benchmark]
        public long GZip_BestSpeed()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                using (var compressedStream = new PooledMemoryStream())
                using (var compressionStream = GZipCompressor_BestSpeed.CreateCompressionStream(compressedStream))
                {
                    serializedStream.Position = 0L;
                    serializedStream.CopyTo(compressionStream);
                    return compressedStream.Length;
                }
            }
        }

        [Benchmark]
        public long Deflate_Default()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                using (var compressedStream = new PooledMemoryStream())
                using (var compressionStream = DeflateCompressor_Default.CreateCompressionStream(compressedStream))
                {
                    serializedStream.Position = 0L;
                    serializedStream.CopyTo(compressionStream);
                    return compressedStream.Length;
                }
            }
        }

        [Benchmark]
        public long Deflate_BestSpeed()
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                JsonSerializer.SerializeToStream(LogMessage.GenerateRandomLogMessages(Count), serializedStream);
                using (var compressedStream = new PooledMemoryStream())
                using (var compressionStream = DeflateCompressor_BestSpeed.CreateCompressionStream(compressedStream))
                {
                    serializedStream.Position = 0L;
                    serializedStream.CopyTo(compressionStream);
                    return compressedStream.Length;
                }
            }
        }
    }
}
