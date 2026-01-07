using System.Collections.Concurrent;

namespace RealTime.Native.Udp.Reliable;

/// <summary>
/// Handles packet sequencing for reliable UDP communication.
/// </summary>
public class PacketSequencer
{
    private long _nextSequenceNumber = 0;
    private readonly ConcurrentDictionary<long, byte[]> _buffer = new();
    private long _expectedSequenceNumber = 0;

    /// <summary>
    /// Gets the next sequence number for outgoing packets.
    /// </summary>
    /// <returns>The next sequence number.</returns>
    public long GetNextSequenceNumber() => Interlocked.Increment(ref _nextSequenceNumber);

    /// <summary>
    /// Processes incoming packets in order, buffering out-of-order packets.
    /// </summary>
    /// <param name="sequenceNumber">The sequence number of the incoming packet.</param>
    /// <param name="data">The data of the incoming packet.</param>
    /// <returns>An enumerable collection of ordered packets ready for processing.</returns>
    public IEnumerable<byte[]> ProcessInOrder(long sequenceNumber, byte[] data)
    {
        if (sequenceNumber < _expectedSequenceNumber) yield break; // Stale packet

        _buffer.TryAdd(sequenceNumber, data);

        // Process all ready packets in order
        while (_buffer.TryRemove(_expectedSequenceNumber, out var nextData))
        {
            yield return nextData;
            _expectedSequenceNumber++;
        }
    }
}
