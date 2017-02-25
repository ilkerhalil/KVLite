// File name: BsonSerializer.cs
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

using Newtonsoft.Json;
using Newtonsoft.Json.Bson;
using PommaLabs.Thrower;
using System.IO;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   BSON serializer based on the <see cref="Newtonsoft.Json.JsonSerializer"/> class.
    /// </summary>
    public sealed class BsonSerializer : ISerializer
    {
        /// <summary>
        ///   Instance of JSON.NET serializer.
        /// </summary>
        private readonly Newtonsoft.Json.JsonSerializer _bsonSerializer;

        /// <summary>
        ///   Builds a BSON serializer using the default settings defined by <see cref="JsonSerializer.DefaultSerializerSettings"/>.
        /// </summary>
        public BsonSerializer()
            : this(JsonSerializer.DefaultSerializerSettings)
        {
        }

        /// <summary>
        ///   Builds a BSON serializer using the specified settings.
        /// </summary>
        /// <param name="serializerSettings">The serializer settings.</param>
        public BsonSerializer(JsonSerializerSettings serializerSettings)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(serializerSettings, nameof(serializerSettings));

            _bsonSerializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static BsonSerializer Instance { get; } = new BsonSerializer();

        /// <summary>
        ///   Determines whether this instance can serialize the specified type.
        /// </summary>
        /// <typeparam name="TObj">The type.</typeparam>
        /// <returns>True if given type is serializable, false otherwise.</returns>
        bool ISerializer.CanSerialize<TObj>() => true; // JSON.NET is able to serialize everything ;-)

        /// <summary>
        ///   Serializes given object into specified stream.
        /// </summary>
        /// <typeparam name="TObj">The type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="outputStream">The output stream.</param>
        public void SerializeToStream<TObj>(TObj obj, Stream outputStream)
        {
#pragma warning disable CC0022 // Should dispose object
            var bsonWriter = new BsonWriter(outputStream);
#pragma warning restore CC0022 // Should dispose object
            _bsonSerializer.Serialize(bsonWriter, new Wrapper<TObj> { Value = obj });
            bsonWriter.Flush();
        }

        /// <summary>
        ///   Determines whether this instance can deserialize the specified type.
        /// </summary>
        /// <typeparam name="TObj">The type.</typeparam>
        /// <returns>True if given type is deserializable, false otherwise.</returns>
        bool ISerializer.CanDeserialize<TObj>() => true; // JSON.NET is able to deserialize everything ;-)

        /// <summary>
        ///   Deserializes the object contained into specified stream.
        /// </summary>
        /// <typeparam name="TObj">The type of the object.</typeparam>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>The deserialized object.</returns>
        public TObj DeserializeFromStream<TObj>(Stream inputStream)
        {
#pragma warning disable CC0022 // Should dispose object
            var bsonReader = new BsonReader(inputStream);
#pragma warning restore CC0022 // Should dispose object
            return _bsonSerializer.Deserialize<Wrapper<TObj>>(bsonReader).Value;
        }

        /// <summary>
        ///   Used to safely wrap primitive types.
        /// </summary>
        /// <typeparam name="TObj">The type.</typeparam>
        private struct Wrapper<TObj>
        {
            /// <summary>
            ///   The value.
            /// </summary>
            public TObj Value { get; set; }
        }
    }
}
