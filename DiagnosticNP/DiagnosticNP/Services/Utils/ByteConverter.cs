using System;
using System.Runtime.InteropServices;

namespace DiagnosticNP.Services.Utils
{
    public static class ByteConverter
    {
        public static T BytesToStruct<T>(this byte[] data)
        {
            GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                handle.Free();
            }
        }

        public static byte[] StructToBytes<T>(this T str)
        {
            int size = Marshal.SizeOf(str);
            byte[] result = new byte[size];
            IntPtr ptr = Marshal.AllocHGlobal(size);
            try
            {
                Marshal.StructureToPtr(str, ptr, true);
                Marshal.Copy(ptr, result, 0, size);
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
            return result;
        }
    }
}