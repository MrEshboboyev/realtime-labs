using RealTime.Native.TcpServer.Models;

namespace RealTime.Native.TcpServer.Events;

public class MessageReceivedEventArgs(TransportPackage package) : EventArgs
{
    public TransportPackage Package { get; } = package;
    public Guid ClientId => Package.ConnectionId;
    public ReadOnlyMemory<byte> Data => Package.Data;
}
