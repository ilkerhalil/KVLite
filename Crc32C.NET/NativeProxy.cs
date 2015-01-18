using System;
using System.Runtime.InteropServices;
using PommaLabs;

namespace Crc32C
{
    abstract class NativeProxy
    {
        public static readonly NativeProxy Instance = IntPtr.Size == 4 ? (NativeProxy)new Native32() : new Native64();

        protected NativeProxy(string name)
        {
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            var snappyPath = (name == "crc32c32.dll") ? "x86/crc32c32.dll" : "x64/crc32c64.dll";
            var h = LoadLibrary(nativePath + snappyPath);
            if (h == IntPtr.Zero)
                throw new ApplicationException("Cannot load " + name);
        }

        public unsafe abstract uint Append(uint crc, byte* input, int length);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);
    }
}
