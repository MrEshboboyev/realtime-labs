using RealTime.Native.Common.Models;
using System.Text;

namespace RealTime.Native.Common.Protocols.Serialization;

public class BinarySerializer : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        if (obj is CommandPackage cmd)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms, Encoding.UTF8);

            writer.Write((int)cmd.Type);      // Enum -> int
            writer.Write(cmd.RoomId ?? "");   // String
            writer.Write(cmd.Content ?? "");  // String
            writer.Write(cmd.SenderName ?? ""); // String

            return ms.ToArray();
        }

        if (obj is string str)
            return Encoding.UTF8.GetBytes(str);

        throw new NotSupportedException($"{typeof(T).Name} uchun serializer topilmadi.");
    }

    public T? Deserialize<T>(byte[] data)
    {
        if (typeof(T) == typeof(CommandPackage))
        {
            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms, Encoding.UTF8);

            var type = (CommandType)reader.ReadInt32();
            var roomId = reader.ReadString();
            var content = reader.ReadString();
            var senderName = reader.ReadString();

            return (T)(object)new CommandPackage(type, roomId, content, senderName);
        }

        if (typeof(T) == typeof(string))
            return (T)(object)Encoding.UTF8.GetString(data);

        return default;
    }
}
