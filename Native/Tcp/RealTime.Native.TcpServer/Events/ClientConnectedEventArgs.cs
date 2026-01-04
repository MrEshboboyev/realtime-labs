using RealTime.Native.TcpServer.Abstractions;

namespace RealTime.Native.TcpServer.Events;

public class ClientConnectedEventArgs(IConnection connection) : EventArgs
{
    public IConnection Connection { get; } = connection;
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
}
