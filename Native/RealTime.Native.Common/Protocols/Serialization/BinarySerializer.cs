using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using System.Text;

namespace RealTime.Native.Common.Protocols.Serialization;

public class BinarySerializer(SharedLogger? logger = null) : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        try
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            if (obj is CommandPackage cmd)
            {
                writer.Write((int)cmd.Type);
                writer.Write(cmd.RoomId ?? "");
                writer.Write(cmd.Content ?? "");
                writer.Write(cmd.SenderName ?? "");
                return ms.ToArray();
            }

            if (obj is UdpPacket udp)
            {
                writer.Write(udp.PacketId.ToByteArray()); // 16 bytes
                writer.Write(udp.SequenceNumber);         // 8 bytes
                writer.Write(udp.RequiresAck);            // 1 byte
                writer.Write(udp.Payload.Length);         // 4 bytes
                writer.Write(udp.Payload);                // N bytes
                return ms.ToArray();
            }

            if (obj is string str)
                return Encoding.UTF8.GetBytes(str);

            throw new NotSupportedException($"Serializer not found for type: {typeof(T).Name}");
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error serializing {typeof(T).Name}", ex);
            throw;
        }
    }

    public T? Deserialize<T>(byte[] data)
    {
        if (data == null || data.Length == 0) return default;

        try
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            if (typeof(T) == typeof(UdpPacket))
            {
                if (data.Length < 16) return default;

                var guid = new Guid(reader.ReadBytes(16));
                var seq = reader.ReadInt64();
                var ack = reader.ReadBoolean();
                var len = reader.ReadInt32();

                if (len > ms.Length - ms.Position) len = (int)(ms.Length - ms.Position);

                var payload = reader.ReadBytes(len);
                return (T)(object)new UdpPacket(guid, seq, payload, ack);
            }

            if (typeof(T) == typeof(CommandPackage))
            {
                var type = (CommandType)reader.ReadInt32();
                var roomId = reader.ReadString();
                var content = reader.ReadString();
                var senderName = reader.ReadString();
                return (T)(object)new CommandPackage(type, roomId, content, senderName);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)Encoding.UTF8.GetString(data);
            }

            return default;
        }
        catch (EndOfStreamException)
        {
            return default;
        }
    }
}
