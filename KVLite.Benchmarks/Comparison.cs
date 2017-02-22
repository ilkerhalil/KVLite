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
using PommaLabs.CodeServices.Caching;
using PommaLabs.CodeServices.Serialization;
using PommaLabs.KVLite.Benchmarks.Compressors;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.SQLite;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;

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

            MySqlCache.DefaultInstance.ConnectionFactory.ConnectionString = ConfigurationManager.ConnectionStrings["MySQL"].ConnectionString;

            _caches = new Dictionary<string, Dictionary<string, Dictionary<string, ICache>>>
            {
                ["mysql"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+gzip" }, serializer: new JsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new BsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new MsgPackSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                },
                ["volatile"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+gzip" }, serializer: new JsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new BsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new MsgPackSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                },
                ["memory"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+gzip" }, serializer: new JsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new BsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new MsgPackSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                },
                ["persistent"] = new Dictionary<string, Dictionary<string, ICache>>
                {
                    ["json"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+gzip" }, serializer: new JsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "json+snappy" }, serializer: new JsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["bson"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new BsonSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new BsonSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new BsonSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new BsonSerializer(), compressor: new SnappyCompressor()),
                    },
                    ["msgpack"] = new Dictionary<string, ICache>
                    {
                        ["deflate"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+deflate" }, serializer: new MsgPackSerializer(), compressor: new DeflateCompressor(CompressionLevel.Fastest)),
                        ["gzip"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+gzip" }, serializer: new MsgPackSerializer(), compressor: new GZipCompressor(CompressionLevel.Fastest)),
                        ["lz4"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+lz4" }, serializer: new MsgPackSerializer(), compressor: new LZ4Compressor()),
                        ["snappy"] = new PersistentCache(new PersistentCacheSettings { DefaultPartition = "bson+snappy" }, serializer: new MsgPackSerializer(), compressor: new SnappyCompressor()),
                    }
                }
            };
        }

        [Params(1, 10, 100)]
        public int Count { get; set; }

        [Params("mysql", "volatile", "memory", "persistent")]
        public string Cache { get; set; }

        [Params("json", "bson", "msgpack")]
        public string Serializer { get; set; }

        [Params("deflate", "gzip", "lz4", "snappy")]
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
