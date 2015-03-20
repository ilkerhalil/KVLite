using System.Runtime.InteropServices;

namespace PommaLabs.KVLite.Core.Crc32C
{
    internal sealed class Native64 : NativeProxy
    {
        public Native64() : base("crc32c64.dll") { }

        public unsafe override uint Append(uint crc, byte* input, int length)
        {
            return crc32c_append(crc, input, checked((ulong)length));
        }

        [DllImport("Crc32C.Interop.dll", CallingConvention = CallingConvention.Cdecl)]
        static unsafe extern uint crc32c_append(uint crc, byte* input, ulong length);
    }
}
