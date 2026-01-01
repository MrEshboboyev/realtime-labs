using RealTime.Native.TcpServer.Events;
using RealTime.Native.TcpServer.Models;

namespace RealTime.Native.TcpServer.Abstractions;

public interface IServer
{
    bool IsRunning { get; }
    Task StartAsync(int port, CancellationToken ct = default);
    Task StopAsync();

    // Voqealar (Events)
    event EventHandler<IConnection> ClientConnected;
    event EventHandler<Guid> ClientDisconnected;
    event EventHandler<TransportPackage> MessageReceived;
}
