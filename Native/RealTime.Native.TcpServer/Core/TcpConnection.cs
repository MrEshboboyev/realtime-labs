using RealTime.Native.Common.Models;
using RealTime.Native.TcpServer.Abstractions;
using System.Net.Sockets;

namespace RealTime.Native.TcpServer.Core;

public class TcpConnection(TcpClient client) : IConnection
{
    public Guid Id { get; } = Guid.NewGuid();
    public TcpClient Client { get; } = client;
    public ConnectionState State { get; private set; } = ConnectionState.Connected;
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;
    public DateTimeOffset LastActivityAt { get; set; } = DateTimeOffset.UtcNow;

    private readonly NetworkStream _stream = client.GetStream();

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (State != ConnectionState.Connected) return;

        // Ma'lumotni tarmoq oqimiga yozish
        await _stream.WriteAsync(data, ct);
        await _stream.FlushAsync(ct);
    }

    public void Close()
    {
        State = ConnectionState.Disconnecting;
        Client.Close();
        State = ConnectionState.Disconnected;
    }

    public void UpdateLastActivity() => LastActivityAt = DateTimeOffset.UtcNow;

    public void Dispose()
    {
        Close();
        _stream.Dispose();
        Client.Dispose();

        GC.SuppressFinalize(this);
    }
}
