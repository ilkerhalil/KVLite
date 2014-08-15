using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using LZ4;

namespace KVLite.Core
{
    internal sealed class BinarySerializer
    {
        // Indicates that types can be stated only for arrays of objects, object members of type Object, and ISerializable non-primitive value types.
        // The XsdString and TypesWhenNeeded settings are meant for high performance serialization between services built on the same version of the .NET Framework. 
        // These two values do not support VTS (Version Tolerant Serialization) because they intentionally omit type information that VTS uses to skip or add optional fields and properties. 
        // You should not use the XsdString or TypesWhenNeeded type formats when serializing and deserializing types on a computer running a different version of the .NET Framework than the computer on which the type was serialized.
        // Serializing and deserializing on computers running different versions of the .NET Framework causes the formatter to skip serialization of type information, 
        // thus making it impossible for the deserializer to skip optional fields if they are not present in certain types that may exist in the other version of the .NET Framework. 
        // If you must use XsdString or TypesWhenNeeded in such a scenario, you must provide custom serialization for types that have changed from one version of the .NET Framework to the other.
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter {TypeFormat = FormatterTypeStyle.TypesWhenNeeded};

        public byte[] SerializeObject(object obj)
        {
            using (var memoryStream = new MemoryStream()) {
                using (var compressor = new LZ4Stream(memoryStream, CompressionMode.Compress)) {
                    _binaryFormatter.Serialize(compressor, obj);
                }
                return memoryStream.GetBuffer();
            }
        }

        public object DeserializeObject(byte[] serialized)
        {
            using (var memoryStream = new MemoryStream(serialized)) {
                using (var decompressor = new LZ4Stream(memoryStream, CompressionMode.Decompress)) {
                    return _binaryFormatter.Deserialize(decompressor);
                }
            }
        }
    }
}