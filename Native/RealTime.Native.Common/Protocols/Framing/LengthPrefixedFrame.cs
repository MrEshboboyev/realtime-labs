using System.Collections.Concurrent;

namespace RealTime.Native.Common.Protocols.Framing;

public class LengthPrefixedFrame : IFrameHandler
{
    // Har bir mijoz uchun chala kelgan baytlarni saqlash
    private readonly ConcurrentDictionary<Guid, List<byte>> _buffers = new();

    public byte[] Wrap(byte[] data)
    {
        byte[] lengthPrefix = BitConverter.GetBytes(data.Length); // 4 bayt
        byte[] frame = new byte[lengthPrefix.Length + data.Length];

        Buffer.BlockCopy(lengthPrefix, 0, frame, 0, lengthPrefix.Length);
        Buffer.BlockCopy(data, 0, frame, lengthPrefix.Length, data.Length);

        return frame;
    }

    public IEnumerable<byte[]> Unwrap(byte[] receivedData, Guid connectionId)
    {
        var connectionBuffer = _buffers.GetOrAdd(connectionId, _ => []);
        connectionBuffer.AddRange(receivedData);

        while (connectionBuffer.Count >= 4)
        {
            // Xabar uzunligini aniqlash (birinchi 4 bayt)
            byte[] lengthBytes = [.. connectionBuffer.GetRange(0, 4)];
            int messageLength = BitConverter.ToInt32(lengthBytes, 0);

            // Agar bufferda to'liq xabar bo'lsa
            if (connectionBuffer.Count >= 4 + messageLength)
            {
                byte[] message = [.. connectionBuffer.GetRange(4, messageLength)];
                connectionBuffer.RemoveRange(0, 4 + messageLength);
                yield return message;
            }
            else
            {
                // Xabar hali to'liq kelmagan, keyingi baytlarni kutamiz
                break;
            }
        }
    }
}
