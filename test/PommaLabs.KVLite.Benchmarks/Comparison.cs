// File name: Comparison.cs
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

using BenchmarkDotNet.Attributes;
using PommaLabs.KVLite.Benchmarks.Models;
using PommaLabs.KVLite.Extensibility;
using PommaLabs.KVLite.Memory;
using PommaLabs.KVLite.MySql;
using PommaLabs.KVLite.SQLite;
using PommaLabs.KVLite.UnitTests;
using System;
using System.Collections.Generic;

namespace PommaLabs.KVLite.Benchmarks
{
    public class Comparison
    {
        private readonly Dictionary<string, Dictionary<string, Dictionary<string, ICache>>> _caches = new Dictionary<string, Dictionary<string, Dictionary<string, ICache>>>
        {
            ["volatile"] = new Dictionary<string, Dictionary<string, ICache>>
            {
                ["json"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+deflate" }, JsonSerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+noop" }, JsonSerializer.Instance, NoOpCompressor.Instance),
                },
                ["binary"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+deflate" }, BinarySerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "binary+noop" }, BinarySerializer.Instance, NoOpCompressor.Instance),
                }
            },
            ["memory"] = new Dictionary<string, Dictionary<string, ICache>>
            {
                ["json"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+deflate" }, JsonSerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "json+noop" }, JsonSerializer.Instance, NoOpCompressor.Instance),
                },
                ["binary"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+deflate" }, BinarySerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new MemoryCache(new MemoryCacheSettings { DefaultPartition = "binary+noop" }, BinarySerializer.Instance, NoOpCompressor.Instance),
                }
            },
            ["mysql"] = new Dictionary<string, Dictionary<string, ICache>>
            {
                ["json"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+deflate", ConnectionString = ConnectionStrings.MySql }, JsonSerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "json+noop", ConnectionString = ConnectionStrings.MySql }, JsonSerializer.Instance, NoOpCompressor.Instance),
                },
                ["binary"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "binary+deflate", ConnectionString = ConnectionStrings.MySql }, BinarySerializer.Instance, DeflateCompressor.Instance),
                    ["noop"] = new MySqlCache(new MySqlCacheSettings { DefaultPartition = "binary+noop", ConnectionString = ConnectionStrings.MySql }, BinarySerializer.Instance, NoOpCompressor.Instance),
                }
            }
        };

        private ICache _cache;

        [Params(1, 10, 100)]
        public int Count { get; set; }

        [Params("volatile", "memory", "mysql")]
        public string Cache { get; set; }

        [Params("json", "binary")]
        public string Serializer { get; set; }

        [Params("deflate", "noop")]
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

        [Benchmark]
        public void GetOrAddManyLogMessages()
        {
            var k = Guid.NewGuid().ToString();
            _cache.GetOrAddStatic(k, () => LogMessage.GenerateRandomLogMessages(Count));
        }
    }
}
