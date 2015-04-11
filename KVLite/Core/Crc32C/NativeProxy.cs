using System;
using System.Runtime.InteropServices;
using PommaLabs.KVLite.Utilities;

namespace PommaLabs.KVLite.Core.Crc32C
{
    internal abstract class NativeProxy
    {
        public static readonly NativeProxy Instance = IntPtr.Size == 4 ? (NativeProxy)new Native32() : new Native64();

        protected NativeProxy(string name)
        {
            var nativePath = (GEnvironment.AppIsRunningOnAspNet ? "bin/KVLite/" : "KVLite/").MapPath();
            var snappyPath = (name == "crc32c32.dll") ? "x86/Crc32C.Interop.dll" : "x64/Crc32C.Interop.dll";
            var h = LoadLibrary(nativePath + snappyPath);
            if (h == IntPtr.Zero)
                throw new ApplicationException("Cannot load " + name);
        }

        public unsafe abstract uint Append(uint crc, byte* input, int length);

        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Unicode)]
        static extern IntPtr LoadLibrary(string lpFileName);
    }
}
