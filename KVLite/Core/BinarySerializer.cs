using CodeProject.ObjectPool.Specialized;
using PommaLabs.CodeServices.Common.Portability;
using PommaLabs.CodeServices.Serialization;
using System.IO;

namespace PommaLabs.KVLite.Core
{
    internal static class BinarySerializer
    {
        /// <summary>
        ///   We pick a value that is the largest multiple of 4096 that is still smaller than the
        ///   large object heap threshold (85K). The copy buffer is short-lived and is likely to be
        ///   collected at Gen0, and it offers a significant improvement in c opy performance.
        /// </summary>
        private const int CopyBufferSize = 81920;

        /// <summary>
        ///   Represents the object data type for cache entries.
        /// </summary>
        private enum DataTypes : byte
        {
            Object = 0,
            ByteArray = 1,
            String = 2
        }

        public static void Serialize<T>(ISerializer serializer, Stream output, T value)
        {
            var maybeBytes = value as byte[];
            if (maybeBytes != null)
            {
                output.WriteByte((byte) DataTypes.ByteArray);
                output.Write(maybeBytes, 0, maybeBytes.Length);
            }
            else
            {
                var maybeString = value as string;
                if (maybeString != null)
                {
                    output.WriteByte((byte) DataTypes.String);
#pragma warning disable CC0022 // Stream is disposed outside this method!
                    var sw = new StreamWriter(output, PortableEncoding.UTF8WithoutBOM, CopyBufferSize);
#pragma warning restore CC0022 // Stream is disposed outside this method!
                    sw.Write(maybeString);
                    sw.Flush();
                }
                else
                {
                    output.WriteByte((byte) DataTypes.Object);
                    serializer.SerializeToStream(value, output);
                }
            }
        }

        public static T Deserialize<T>(ISerializer serializer, IMemoryStreamPool memoryStreamPool, Stream input)
        {
            var dataType = (DataTypes) input.ReadByte();
            switch (dataType)
            {
                case DataTypes.Object:
                    return serializer.DeserializeFromStream<T>(input);

                case DataTypes.String:
#pragma warning disable CC0022 // Stream is disposed outside this method!
                    var sr = new StreamReader(input, PortableEncoding.UTF8WithoutBOM, false, CopyBufferSize);
#pragma warning restore CC0022 // Stream is disposed outside this method!
                    return (T) (object) sr.ReadToEnd();

                case DataTypes.ByteArray:
                    using (var ms = memoryStreamPool.GetObject().MemoryStream)
                    {
                        input.CopyTo(ms, CopyBufferSize);
                        return (T) (object) ms.ToArray();
                    }

                default:
                    throw new InvalidDataException(ErrorMessages.InvalidDataType);
            }
        }
    }
}
