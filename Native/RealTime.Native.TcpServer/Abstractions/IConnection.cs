using RealTime.Native.Common.Models;
using System.Net.Sockets;

namespace RealTime.Native.TcpServer.Abstractions;

public interface IConnection : IDisposable
{
    Guid Id { get; }
    TcpClient Client { get; }
    ConnectionState State { get; }
    DateTimeOffset ConnectedAt { get; }
    DateTimeOffset LastActivityAt { get; set; }

    // Ma'lumot yuborish
    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);

    // Ulanishni yopish
    void Close();

    void UpdateLastActivity();
}
