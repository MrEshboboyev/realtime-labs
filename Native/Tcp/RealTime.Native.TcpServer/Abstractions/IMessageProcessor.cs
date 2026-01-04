using RealTime.Native.Common.Models;

namespace RealTime.Native.TcpServer.Abstractions;

public interface IMessageProcessor
{
    // Kelgan paketni qayta ishlash
    Task ProcessAsync(IConnection connection, TransportPackage package);
}
