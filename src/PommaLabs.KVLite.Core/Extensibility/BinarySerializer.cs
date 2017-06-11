// File name: BinarySerializer.cs
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

#if !NETSTD13

using PommaLabs.KVLite.Thrower;
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;

namespace PommaLabs.KVLite.Extensibility
{
    /// <summary>
    ///   A serializer based on the <see cref="BinaryFormatter"/> class.
    /// </summary>
    public sealed class BinarySerializer : ISerializer
    {
        /// <summary>
        ///   Instance of .NET binary formatter.
        /// </summary>
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        /// <summary>
        ///   Builds a binary serializer using the default settings defined by <see cref="DefaultSerializerSettings"/>.
        /// </summary>
        public BinarySerializer()
            : this(DefaultSerializerSettings)
        {
        }

        /// <summary>
        ///   Builds a binary serializer using the specified settings.
        /// </summary>
        /// <param name="serializerSettings">The serializer settings.</param>
        public BinarySerializer(BinarySerializerSettings serializerSettings)
        {
            // Preconditions
            Raise.ArgumentNullException.IfIsNull(serializerSettings, nameof(serializerSettings));

            _binaryFormatter.AssemblyFormat = serializerSettings.AssemblyFormat;
            _binaryFormatter.Binder = serializerSettings.Binder ?? _binaryFormatter.Binder;
            _binaryFormatter.FilterLevel = serializerSettings.FilterLevel;
            _binaryFormatter.SurrogateSelector = serializerSettings.SurrogateSelector ?? _binaryFormatter.SurrogateSelector;
            _binaryFormatter.TypeFormat = serializerSettings.TypeFormat;
        }

        /// <summary>
        ///   Default binary serializer settings, used when none has been specified.
        /// </summary>
        public static BinarySerializerSettings DefaultSerializerSettings = new BinarySerializerSettings
        {
            AssemblyFormat = FormatterAssemblyStyle.Simple,
            FilterLevel = TypeFilterLevel.Full,
            TypeFormat = FormatterTypeStyle.TypesWhenNeeded
        };

        /// <summary>
        ///   Thread safe singleton.
        /// </summary>
        public static BinarySerializer Instance { get; } = new BinarySerializer();

        /// <summary>
        ///   Determines whether this instance can serialize the specified type.
        /// </summary>
        /// <typeparam name="TObj">The type.</typeparam>
        /// <returns>True if given type is serializable, false otherwise.</returns>
        public bool CanSerialize<TObj>() => typeof(TObj).IsSerializable;

        /// <summary>
        ///   Serializes given object into specified stream.
        /// </summary>
        /// <typeparam name="TObj">The type of the object.</typeparam>
        /// <param name="obj">The object.</param>
        /// <param name="outputStream">The output stream.</param>
        public void SerializeToStream<TObj>(TObj obj, Stream outputStream)
        {
            if (ReferenceEquals(obj, null))
            {
                // Workaround to make binary formatter handle null objects.
                _binaryFormatter.Serialize(outputStream, new NullObject());
            }
            else
            {
                _binaryFormatter.Serialize(outputStream, obj);
            }
        }

        /// <summary>
        ///   Determines whether this instance can deserialize the specified type.
        /// </summary>
        /// <typeparam name="TObj">The type.</typeparam>
        /// <returns>True if given type is deserializable, false otherwise.</returns>
        public bool CanDeserialize<TObj>() => typeof(TObj).IsSerializable;

        /// <summary>
        ///   Deserializes the object contained into specified stream.
        /// </summary>
        /// <typeparam name="TObj">The type of the object.</typeparam>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>The deserialized object.</returns>
        public TObj DeserializeFromStream<TObj>(Stream inputStream)
        {
            var obj = _binaryFormatter.Deserialize(inputStream);
            if (obj is NullObject)
            {
                return default(TObj);
            }
            if (!typeof(TObj).IsInstanceOfType(obj))
            {
                throw new InvalidCastException();
            }
            return (TObj) obj;
        }

        /// <summary>
        ///   Workaround to make binary formatter handle null objects.
        /// </summary>
        [Serializable]
        private struct NullObject
        {
        }
    }

    /// <summary>
    ///   Settings for <see cref="BinarySerializer"/>.
    /// </summary>
    public sealed class BinarySerializerSettings
    {
        /// <summary>
        ///   Gets or sets the behavior of the deserializer with regards to finding and loading assemblies.
        /// </summary>
        /// <returns>
        ///   One of the <see cref="FormatterAssemblyStyle"/> values that specifies the deserializer behavior.
        /// </returns>
        public FormatterAssemblyStyle AssemblyFormat { get; set; }

        /// <summary>
        ///   Gets or sets an object of type SerializationBinder that controls the binding of a
        ///   serialized object to a type.
        /// </summary>
        /// <value>
        ///   An object of type SerializationBinder that controls the binding of a serialized object
        ///   to a type.
        /// </value>
        public SerializationBinder Binder { get; set; }

        /// <summary>
        ///   Gets or sets the TypeFilterLevel of automatic deserialization the BinaryFormatter performs.
        /// </summary>
        /// <value>The TypeFilterLevel of automatic deserialization the BinaryFormatter performs.</value>
        public TypeFilterLevel FilterLevel { get; set; }

        /// <summary>
        ///   Gets or sets an ISurrogateSelector that controls type substitution during serialization
        ///   and deserialization.
        /// </summary>
        /// <value>
        ///   An ISurrogateSelector that controls type substitution during serialization and deserialization.
        /// </value>
        public ISurrogateSelector SurrogateSelector { get; set; }

        /// <summary>
        ///   Gets or sets the format in which type descriptions are laid out in the serialized stream.
        /// </summary>
        /// <value>The format in which type descriptions are laid out in the serialized stream.</value>
        public FormatterTypeStyle TypeFormat { get; set; }
    }
}

#endif
