using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using System.Text;

namespace RealTime.Native.Common.Protocols.Serialization;

/// <summary>
/// Provides binary serialization for common types including CommandPackage and string
/// </summary>
public class BinarySerializer(
    SharedLogger? logger = null
) : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        try
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

            var errorMessage = $"Serializer not found for type: {typeof(T).Name}";
            logger?.Log(LogLevel.Error, errorMessage);
            throw new NotSupportedException(errorMessage);
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error serializing object of type {typeof(T).Name}", ex);
            throw;
        }
    }

    public T? Deserialize<T>(byte[] data)
    {
        try
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

            logger?.Log(LogLevel.Warning, $"Deserializer not implemented for type: {typeof(T).Name}");
            return default;
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error deserializing to type {typeof(T).Name}", ex);
            throw;
        }
    }
}
