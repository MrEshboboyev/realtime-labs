using System.Buffers;

namespace RealTime.Native.Common.Infrastructure;

/// <summary>
/// Provides a shared buffer pool for network operations to reduce memory allocations
/// </summary>
public class NetworkBufferPool
{
    // Singleton pattern to ensure all components use the same pool
    public static NetworkBufferPool Shared { get; } = new();

    private readonly ArrayPool<byte> _pool = ArrayPool<byte>.Shared;

    /// <summary>
    /// Rents a buffer from the pool with at least the specified minimum size
    /// </summary>
    /// <param name="minSize">The minimum size of the buffer to rent</param>
    /// <returns>A buffer of at least minSize bytes</returns>
    public byte[] Rent(int minSize) => _pool.Rent(minSize);

    /// <summary>
    /// Returns a buffer to the pool
    /// </summary>
    /// <param name="array">The buffer to return</param>
    /// <param name="clearArray">Whether to clear the array before returning to prevent data leaks</param>
    public void Return(byte[] array, bool clearArray = false) => _pool.Return(array, clearArray);
}
