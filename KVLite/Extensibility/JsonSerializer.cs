﻿// File name: JsonSerializer.cs
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
using PommaLabs.Thrower;
using System.IO;
using System.Runtime.Serialization.Formatters;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   JSON serializer based on the <see cref="Newtonsoft.Json.JsonSerializer"/> class.
    /// </summary>
    public sealed class JsonSerializer : ISerializer
    {
        /// <summary>
        ///   Instance of JSON.NET serializer.
        /// </summary>
        private readonly Newtonsoft.Json.JsonSerializer _jsonSerializer;

        /// <summary>
        ///   Builds a JSON serializer using the default settings defined by <see cref="DefaultSerializerSettings"/>.
        /// </summary>
        public JsonSerializer()
            : this(DefaultSerializerSettings)
        {
        }

        /// <summary>
        ///   Builds a JSON serializer using the specified settings.
        /// </summary>
        /// <param name="serializerSettings">The serializer settings.</param>
        public JsonSerializer(JsonSerializerSettings serializerSettings)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(serializerSettings, nameof(serializerSettings));

            _jsonSerializer = Newtonsoft.Json.JsonSerializer.Create(serializerSettings);
        }

        /// <summary>
        ///   Default JSON serializer settings, used when none has been specified.
        /// </summary>
        /// <remarks>
        ///   We apply many customizations to the JSON serializer, in order to achieve a small output size.
        /// </remarks>
        public static JsonSerializerSettings DefaultSerializerSettings = new JsonSerializerSettings
        {
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
            DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate,
            FloatFormatHandling = FloatFormatHandling.String,
            Formatting = Formatting.None,
            MissingMemberHandling = MissingMemberHandling.Ignore,
            NullValueHandling = NullValueHandling.Ignore,
            PreserveReferencesHandling = PreserveReferencesHandling.None,
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            TypeNameHandling = TypeNameHandling.All,
            TypeNameAssemblyFormat = FormatterAssemblyStyle.Full
        };

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static JsonSerializer Instance { get; } = new JsonSerializer();

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
            var streamWriter = new StreamWriter(outputStream);
            var jsonWriter = new JsonTextWriter(streamWriter);
#pragma warning restore CC0022 // Should dispose object
            _jsonSerializer.Serialize(jsonWriter, obj);
            jsonWriter.Flush();
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
            var streamReader = new StreamReader(inputStream);
            var jsonReader = new JsonTextReader(streamReader);
#pragma warning restore CC0022 // Should dispose object
            return _jsonSerializer.Deserialize<TObj>(jsonReader);
        }
    }
}