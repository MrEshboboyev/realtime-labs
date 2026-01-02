using System.Buffers;

namespace RealTime.Native.Common.Infrastructure;

public class NetworkBufferPool
{
    // Singleton pattern orqali barcha qismlar bir xil pool'dan foydalanishi uchun
    public static NetworkBufferPool Shared { get; } = new();

    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    public byte[] Rent(int minSize) => _pool.Rent(minSize);

    public void Return(byte[] array, bool clearArray = false) => _pool.Return(array, clearArray);
}
