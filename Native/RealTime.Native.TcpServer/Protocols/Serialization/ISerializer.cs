namespace RealTime.Native.TcpServer.Protocols.Serialization;

public interface ISerializer
{
    byte[] Serialize<T>(T obj);
    T? Deserialize<T>(byte[] data);
}
