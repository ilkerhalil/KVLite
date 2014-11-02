//
// BinarySerializer.cs
// 
// Author(s):
//     Alessio Parma <alessio.parma@gmail.com>
//
// The MIT License (MIT)
// 
// Copyright (c) 2014-2015 Alessio Parma <alessio.parma@gmail.com>
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using CodeProject.ObjectPool;

namespace PommaLabs.KVLite.Core
{
    internal static class BinarySerializer
    {
        private static readonly ObjectPool<PooledObjectWrapper<NetDataContractSerializer>> FormatterPool = new ObjectPool<PooledObjectWrapper<NetDataContractSerializer>>(
            1, Configuration.Instance.MaxCachedSerializerCount, CreatePooledBinaryFormatter);

        public static byte[] SerializeObject(object obj)
        {
            using (var compressedStream = new MemoryStream()) {
                using (var decompressedStream = new DeflateStream(compressedStream, CompressionMode.Compress)) {
                    using (var binaryFormatter = FormatterPool.GetObject()) {
                        try
                        {
                            binaryFormatter.InternalResource.Serialize(decompressedStream, obj);
                        }
                        catch (Exception ex)
                        {
                            throw new ArgumentException(ErrorMessages.NotSerializableValue, ex);
                        }
                    }
                }
                return compressedStream.GetBuffer();
            }
        }

        public static object DeserializeObject(byte[] serialized)
        {
            using (var compressedStream = new MemoryStream(serialized)) {
                using (var decompressedStream = new DeflateStream(compressedStream, CompressionMode.Decompress)) {
                    using (var binaryFormatter = FormatterPool.GetObject()) {
                        return binaryFormatter.InternalResource.Deserialize(decompressedStream);
                    }
                }
            }
        }

        private static PooledObjectWrapper<NetDataContractSerializer> CreatePooledBinaryFormatter()
        {
            var formatter = new NetDataContractSerializer {
                // In simple mode, the assembly used during deserialization need not match exactly the assembly used during serialization. 
                // Specifically, the version numbers need not match as the LoadWithPartialName method is used to load the assembly.
                AssemblyFormat = FormatterAssemblyStyle.Simple                
            };
            return new PooledObjectWrapper<NetDataContractSerializer>(formatter);
        }
    }
}