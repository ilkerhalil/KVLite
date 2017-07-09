// File name: Comparison.cs
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

using BenchmarkDotNet.Attributes;
using PommaLabs.KVLite.Benchmarks.Compression;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.UnitTests;
using System;
using System.Collections.Generic;
using System.IO;

namespace PommaLabs.KVLite.Benchmarks
{
    public class Comparison
    {
        private Dictionary<string, Dictionary<string, Dictionary<string, ICache>>> _caches;
        private ICache _cache;

        public Comparison()
        {
            Directory.CreateDirectory("BenchmarkDotNet.Artifacts\\bin\\BDN.Auto\\x86");
            Directory.CreateDirectory("BenchmarkDotNet.Artifacts\\bin\\BDN.Auto\\x64");
            var src86 = Path.GetFullPath("x86\\SQLite.Interop.dll");
            var src64 = Path.GetFullPath("x64\\SQLite.Interop.dll");
            var dst86 = Path.Combine(Environment.CurrentDirectory, "BenchmarkDotNet.Artifacts\\bin\\BDN.Auto\\x86\\SQLite.Interop.dll");
            var dst64 = Path.Combine(Environment.CurrentDirectory, "BenchmarkDotNet.Artifacts\\bin\\BDN.Auto\\x64\\SQLite.Interop.dll");
            File.Copy(src86, dst86, true);
            File.Copy(src64, dst64, true);

            _caches = new Dictionary<string, Dictionary<string, Dictionary<string, ICache>>>
            {
                ["volatile"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+deflate" }, JsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+gzip" }, JsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+lz4" }, JsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+noop" }, JsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+deflate" }, BsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+gzip" }, BsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+lz4" }, BsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+noop" }, BsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["binary"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+deflate" }, BinarySerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+gzip" }, BinarySerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+lz4" }, BinarySerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+noop" }, BinarySerializer.Instance, NoOpCompressor.Instance),
                    }
                },
                ["memory"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+deflate" }, JsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+gzip" }, JsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+lz4" }, JsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+noop" }, JsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+deflate" }, BsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+gzip" }, BsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+lz4" }, BsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+noop" }, BsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["binary"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+deflate" }, BinarySerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+gzip" }, BinarySerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+lz4" }, BinarySerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+noop" }, BinarySerializer.Instance, NoOpCompressor.Instance),
                    }
                },
                ["persistent"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+deflate" }, JsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+gzip" }, JsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+lz4" }, JsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+noop" }, JsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+deflate" }, BsonSerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+gzip" }, BsonSerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+lz4" }, BsonSerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+noop" }, BsonSerializer.Instance, NoOpCompressor.Instance),
                    },
                    ["binary"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "binary+deflate" }, BinarySerializer.Instance, DeflateCompressor.Instance),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "binary+gzip" }, BinarySerializer.Instance, GZipCompressor.Instance),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "binary+lz4" }, BinarySerializer.Instance, LZ4Compressor.Instance),
                        ["noop"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "binary+noop" }, BinarySerializer.Instance, NoOpCompressor.Instance),
                    }
                }
            };
        }

        [Params(1, 10, 100)]
        public int Count { get; set; }

        [Params("volatile", "memory", "persistent")]
        public string Cache { get; set; }

        [Params("json", "bson", "binary")]
        public string Serializer { get; set; }

        [Params("deflate", "gzip", "lz4", "noop")]
        public string Compressor { get; set; }

        [GlobalSetup]
        public void ClearCache()
        {
            _cache = _caches[Cache][Serializer][Compressor];
            _cache.Clear();
        }

        [Benchmark]
        public void AddManyLogMessages()
        {
            var k = Guid.NewGuid().ToString();
            _cache.AddStatic(k, LogMessage.GenerateRandomLogMessages(Count));
        }
    }
}
