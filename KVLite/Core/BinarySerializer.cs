using PommaLabs.CodeServices.Serialization;
using System;
using System.IO;
using System.Text;

namespace PommaLabs.KVLite.Core
{
    internal static class BinarySerializer
    {
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
                    var maybeStringBytes = Encoding.Default.GetBytes(maybeString);
                    output.Write(maybeStringBytes, 0, maybeStringBytes.Length);
                }
                else
                {
                    output.WriteByte((byte) DataTypes.Object);
                    serializer.SerializeToStream(value, output);
                }
            }
        }

        public static T Deserialize<T>(ISerializer serializer, Stream input)
        {
            var dataType = (DataTypes) input.ReadByte();
            switch (dataType)
            {
                case DataTypes.Object:
                    return serializer.DeserializeFromStream<T>(input);

                case DataTypes.String:
                    using (var br = new BinaryReader(input))
                    {
                        return (T) (object) Encoding.Default.GetString(br.ReadBytes((int) (input.Length - input.Position)));
                    }

                case DataTypes.ByteArray:
                    using (var br = new BinaryReader(input))
                    {
                        return (T) (object) br.ReadBytes((int) (input.Length - input.Position));
                    }

                default:
                    throw new InvalidDataException(ErrorMessages.InvalidDataType);
            }
        }
    }
}
