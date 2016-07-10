// File name: Comparison.cs
//
// Author(s): Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
//
// Copyright (c) 2014-2016 Alessio Parma <alessio.parma@gmail.com>
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
using Finsa.CodeServices.Caching;
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using Ionic.Zlib;
using PommaLabs.KVLite.Benchmarks.Models;
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
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+zlib" }, serializer: new JsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new BsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new MsgPackSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                },
                ["memory"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+zlib" }, serializer: new JsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new BsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new MsgPackSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                },
                ["persistent"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+zlib" }, serializer: new JsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new BsonSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.BestSpeed)),
                        ["zlib"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+zlib" }, serializer: new MsgPackSerializer(), compressor: new ZlibCompressor(CompressionLevel.BestSpeed)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                }
            };
        }

        [Params(1, 10, 100)]
        public int Count { get; set; }

        [Params("volatile", "memory", "persistent")]
        public string Cache { get; set; }

        [Params("json", "bson", "msgpack")]
        public string Serializer { get; set; }

        [Params("deflate", "zlib", "lz4", "snappy")]
        public string Compressor { get; set; }

        [Setup]
        public void ClearCache()
        {
            _cache = _caches[Cache][Serializer][Compressor];
            _cache.Clear();
        }

        [Benchmark]
        public void AddManyLogMessages()
        {            
            var k = Guid.NewGuid().ToString();
            _cache.AddStaticToDefaultPartition(k, LogMessage.GenerateRandomLogMessages(Count));
        }
    }
}
