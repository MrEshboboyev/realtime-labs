using System.Runtime.InteropServices;

namespace RealTime.Native.TcpServer.Protocols.Serialization;

public class BinarySerializer : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        // Oddiy string yoki value-type'larni binaryga o'girish mantiqi
        if (obj is string str)
            return System.Text.Encoding.UTF8.GetBytes(str);

        // Murakkab ob'ektlar uchun struct -> byte[] konvertatsiyasi
        int size = Marshal.SizeOf(obj!);
        byte[] arr = new byte[size];
        IntPtr ptr = Marshal.AllocHGlobal(size);

        Marshal.StructureToPtr(obj!, ptr, true);
        Marshal.Copy(ptr, arr, 0, size);
        Marshal.FreeHGlobal(ptr);

        return arr;
    }

    public T? Deserialize<T>(byte[] data)
    {
        if (typeof(T) == typeof(string))
            return (T)(object)System.Text.Encoding.UTF8.GetString(data);

        GCHandle handle = GCHandle.Alloc(data, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }
}
