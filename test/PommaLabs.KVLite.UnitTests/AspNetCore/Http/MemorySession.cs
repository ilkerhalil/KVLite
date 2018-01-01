﻿// File name: MemorySession.cs
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

#if HAS_ASPNETCORE

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace PommaLabs.KVLite.UnitTests.AspNetCore.Http
{
    internal sealed class MemorySession : ISession
    {
        private readonly string _id = Guid.NewGuid().ToString();
        private readonly Dictionary<string, byte[]> _session = new Dictionary<string, byte[]>();

        public string Id => _id;

        public bool IsAvailable => true;

        public IEnumerable<string> Keys => _session.Keys;

        public void Clear() => _session.Clear();

        public Task CommitAsync(CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default(CancellationToken)) => Task.CompletedTask;

        public void Remove(string key) => _session.Remove(key);

        public void Set(string key, byte[] value) => _session[key] = value;

        public bool TryGetValue(string key, out byte[] value) => _session.TryGetValue(key, out value);
    }
}

#endif
