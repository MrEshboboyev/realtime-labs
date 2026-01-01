using System.Buffers;

namespace RealTime.Native.TcpServer.Infrastructure;

public class BufferPool(int defaultBufferSize = 4096)
{
    public byte[] Rent() => ArrayPool<byte>.Shared.Rent(defaultBufferSize);

    public void Return(byte[] array) => ArrayPool<byte>.Shared.Return(array);
}
