using System.Runtime.InteropServices;

namespace PommaLabs.KVLite.Core.Crc32C
{
    internal sealed class Native32 : NativeProxy
    {
        public Native32() : base("crc32c32.dll") { }

        public unsafe override uint Append(uint crc, byte* input, int length)
        {
            return crc32c_append(crc, input, checked((uint)length));
        }

        [DllImport("Crc32C.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern uint crc32c_append(uint crc, byte* input, uint length);
    }
}
