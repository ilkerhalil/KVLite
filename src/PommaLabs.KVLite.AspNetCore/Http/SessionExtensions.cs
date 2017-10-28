// File name: SessionExtensions.cs
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

using Microsoft.AspNetCore.Http;
using PommaLabs.KVLite.Core;
using PommaLabs.KVLite.Extensibility;

namespace PommaLabs.KVLite.AspNetCore.Http
{
    /// <summary>
    ///   Extension methods for <see cref="ISession"/> interface.
    /// </summary>
    public static class SessionExtensions
    {
        public static void SetObject<T>(this ISession session, string key, T value) => SetObject(session, JsonSerializer.Instance, key, value);

        public static void SetObject<T>(this ISession session, ISerializer serializer, string key, T value)
        {
            using (var serializedStream = new PooledMemoryStream())
            {
                BlobSerializer.Serialize(serializer, serializedStream, value);
                session.Set(key, serializedStream.ToArray());
            }
        }

        public static CacheResult<T> GetObject<T>(this ISession session, string key) => GetObject<T>(session, JsonSerializer.Instance, key);

        public static CacheResult<T> GetObject<T>(this ISession session, ISerializer serializer, string key)
        {
            if (session.TryGetValue(key, out var serializedBytes))
            {
                using (var serializedStream = new PooledMemoryStream(serializedBytes))
                {
                    return BlobSerializer.Deserialize<T>(serializer, serializedStream);
                }
            }
            return default(CacheResult<T>);
        }
    }
}
