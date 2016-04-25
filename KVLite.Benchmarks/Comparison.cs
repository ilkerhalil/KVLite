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
using Finsa.CodeServices.Compression;
using Finsa.CodeServices.Serialization;
using PommaLabs.KVLite.Benchmarks.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace PommaLabs.KVLite.Benchmarks
{
    public class Comparison
    {
        private readonly Dictionary<string, Dictionary<string, ICache>> _caches;

        public Comparison()
        {
            Directory.CreateDirectory("x86");
            Directory.CreateDirectory("x64");
            var src86 = Path.GetFullPath("x86\\SQLite.Interop.dll");
            var src64 = Path.GetFullPath("x64\\SQLite.Interop.dll");
            var dst86 = Path.Combine(Environment.CurrentDirectory, "x86\\SQLite.Interop.dll");
            File.Copy(src86, dst86);
            File.Copy(src64, "x64\\SQLite.Interop.dll");

            _caches = new Dictionary<string, Dictionary<string, ICache>>
            {
                ["json"] = new Dictionary<string, ICache>
                {
                    ["deflate"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+deflate" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor()),
                    ["lz4"] = new VolatileCache(new VolatileCacheSettings { DefaultPartition = "json+lz4" }, serializer: new JsonSerializer(), compressor: new DeflateCompressor())
                }
            };
        }

        [Params(1, 10, 100)]
        public int Count { get; set; }

        [Params("json")]
        public string Serializer { get; set; }

        [Params("deflate", "lz4")]
        public string Compressor { get; set; }

        [Setup]
        public void ClearCache()
        {
            (_caches[Serializer][Compressor] as VolatileCache).Clear(CacheReadMode.IgnoreExpiryDate);
        }

        [Benchmark]
        public void AddManyLogMessages()
        {
            var k = Guid.NewGuid().ToString();
            _caches[Serializer][Compressor].AddStaticToDefaultPartition(k, LogMessage.GenerateRandomLogMessages(Count));
        }
    }
}