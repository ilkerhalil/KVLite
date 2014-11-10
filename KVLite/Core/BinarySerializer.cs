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

using System.IO;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using CodeProject.DeepXmlSerializer;

namespace PommaLabs.KVLite.Core
{
    internal static class BinarySerializer
    {
        private const SaveOptions XmlSaveOptions = SaveOptions.DisableFormatting | SaveOptions.OmitDuplicateNamespaces; 

        public static byte[] SerializeObject(object obj)
        {
            using (var compressedStream = new MemoryStream()) {
                using (var decompressedStream = new DeflateStream(compressedStream, CompressionMode.Compress)) {
                    DeepXmlSerializer.Serialize(obj, 0).Save(decompressedStream, XmlSaveOptions);
                }
                return compressedStream.GetBuffer();
            }
        }

        public static object DeserializeObject(byte[] serialized)
        {
            using (var compressedStream = new MemoryStream(serialized)) {
                using (var decompressedStream = new DeflateStream(compressedStream, CompressionMode.Decompress)) {
                    using (var streamReader = new StreamReader(decompressedStream, Encoding.UTF8)) {
                        return DeepXmlDeserializer.Deserialize(streamReader.ReadToEnd(), 0);
                    }
                }
            }
        }
    }
}