using System.Collections.Concurrent;

namespace RealTime.Native.Udp.Reliable;

public class PacketSequencer
{
    private long _nextSequenceNumber = 0;
    private readonly ConcurrentDictionary<long, byte[]> _buffer = new();
    private long _expectedSequenceNumber = 0;

    // Yuboruvchi uchun: Keyingi tartib raqamini olish
    public long GetNextSequenceNumber() => Interlocked.Increment(ref _nextSequenceNumber);

    // Qabul qiluvchi uchun: Paketni tartib bo'yicha qayta ishlash
    public IEnumerable<byte[]> ProcessInOrder(long seqNum, byte[] data)
    {
        if (seqNum < _expectedSequenceNumber) yield break; // Eskirgan paket

        _buffer.TryAdd(seqNum, data);

        // Tartib bo'yicha tayyor bo'lgan barcha paketlarni chiqaramiz
        while (_buffer.TryRemove(_expectedSequenceNumber, out var nextData))
        {
            yield return nextData;
            _expectedSequenceNumber++;
        }
    }
}
