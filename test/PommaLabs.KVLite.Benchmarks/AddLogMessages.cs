﻿// File name: AddLogMessages.cs
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
using System;

namespace PommaLabs.KVLite.Benchmarks
{
    public class AddLogMessages
    {
        [Params(10, 100)]
        public int Count { get; set; }

        [GlobalSetup]
        public void ClearCaches() => Caches.Clear();

        [Benchmark]
        public void MemoryCache() => AddLogMsg(Caches.Memory);

        [Benchmark(Baseline = true)]
        public void MySqlCache() => AddLogMsg(Caches.MySql);

        [Benchmark]
        public void PostgreSqlCache() => AddLogMsg(Caches.PostgreSql);

        private void AddLogMsg<TCache>(TCache cache)
            where TCache : ICache
        {
            var k = Guid.NewGuid().ToString();
            cache.AddStatic(k, LogMessage.GenerateRandomLogMessages(Count));
        }
    }
}