using System.IO;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using KVLite.Compression;

namespace KVLite.Core
{
    internal sealed class BinarySerializer
    {
        private readonly BinaryFormatter _binaryFormatter = new BinaryFormatter();

        public BinarySerializer()
        {
            // Indicates that strings can be given in the XSD format rather than SOAP. No string IDs are transmitted.
            // The XsdString and TypesWhenNeeded settings are meant for high performance serialization between services built on the same version of the .NET Framework. 
            // These two values do not support VTS (Version Tolerant Serialization) because they intentionally omit type information that VTS uses to skip or add optional fields and properties. 
            // You should not use the XsdString or TypesWhenNeeded type formats when serializing and deserializing types on a computer running a different version of the .NET Framework than the computer on which the type was serialized.
            // Serializing and deserializing on computers running different versions of the .NET Framework causes the formatter to skip serialization of type information, 
            // thus making it impossible for the deserializer to skip optional fields if they are not present in certain types that may exist in the other version of the .NET Framework. 
            // If you must use XsdString or TypesWhenNeeded in such a scenario, you must provide custom serialization for types that have changed from one version of the .NET Framework to the other.
            _binaryFormatter.TypeFormat = FormatterTypeStyle.XsdString;
        }

        public byte[] SerializeObject(object obj)
        {
            using (var memoryStream = new MemoryStream())
            {
                _binaryFormatter.Serialize(memoryStream, obj);
                // Level 1 gives the fastest compression speed while level 3 gives the fastest decompression speed.
                return QuickLZ.compress(memoryStream.ToArray(), 3);
            }          
        }

        public object DeserializeObject(byte[] serialized)
        {
            using (var memoryStream = new MemoryStream(QuickLZ.decompress(serialized)))
            {
                return _binaryFormatter.Deserialize(memoryStream);
            }
        }
    }
}
