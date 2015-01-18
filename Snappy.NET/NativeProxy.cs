using System;
using System.Runtime.InteropServices;
using PommaLabs;

namespace Snappy
{
    abstract class NativeProxy
    {
        public static readonly NativeProxy Instance = IntPtr.Size == 4 ? (NativeProxy)new Native32() : new Native64();

        protected NativeProxy(string name)
        {
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            var snappyPath = (name == "snappy32.dll") ? "x86/snappy32.dll" : "x64/snappy64.dll";
            var h = LoadLibrary(nativePath + snappyPath);
            if (h == IntPtr.Zero)
                throw new ApplicationException("Cannot load " + name);
        }

        public unsafe abstract SnappyStatus Compress(byte* input, int inLength, byte* output, ref int outLength);
        public unsafe abstract SnappyStatus Uncompress(byte* input, int inLength, byte* output, ref int outLength);
        public abstract int GetMaxCompressedLength(int inLength);
        public unsafe abstract SnappyStatus GetUncompressedLength(byte* input, int inLength, out int outLength);
        public unsafe abstract SnappyStatus ValidateCompressedBuffer(byte* input, int inLength);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);
    }
}
