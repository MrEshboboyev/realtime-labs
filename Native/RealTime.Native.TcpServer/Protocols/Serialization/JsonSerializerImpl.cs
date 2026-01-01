using System.Text;
using System.Text.Json;

namespace RealTime.Native.TcpServer.Protocols.Serialization;

public class JsonSerializerImpl : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        string json = JsonSerializer.Serialize(obj);
        return Encoding.UTF8.GetBytes(json);
    }

    public T? Deserialize<T>(byte[] data)
    {
        string json = Encoding.UTF8.GetString(data);
        return JsonSerializer.Deserialize<T>(json);
    }
}
