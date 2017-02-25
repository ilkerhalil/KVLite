// File name: BinarySerializerSettings.cs
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

using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;

namespace PommaLabs.KVLite.WebForms
{
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
