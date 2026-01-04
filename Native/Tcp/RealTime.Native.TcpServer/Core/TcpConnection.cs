using RealTime.Native.Common.Models;
using RealTime.Native.TcpServer.Abstractions;
using System.Net.Sockets;
using RealTime.Native.Common.Infrastructure;

namespace RealTime.Native.TcpServer.Core;

/// <summary>
/// Represents a TCP connection to a client
/// </summary>
public class TcpConnection : IConnection
{
    private readonly SharedLogger? _logger;
    
    public Guid Id { get; }
    public TcpClient Client { get; }
    public ConnectionState State { get; private set; }
    public DateTimeOffset ConnectedAt { get; }
    public DateTimeOffset LastActivityAt { get; set; }

    private readonly NetworkStream _stream;
    private bool _disposed = false;
    
    public TcpConnection(TcpClient client, SharedLogger? logger = null)
    {
        Id = Guid.NewGuid();
        Client = client;
        _logger = logger;
        State = ConnectionState.Connected;
        ConnectedAt = DateTimeOffset.UtcNow;
        LastActivityAt = DateTimeOffset.UtcNow;
        
        _stream = client.GetStream();
        
        // Set socket options
        Client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
    }

    public async Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default)
    {
        if (_disposed)
        {
            _logger?.Log(LogLevel.Warning, $"Attempted to send data on disposed connection {Id}");
            return;
        }
        
        if (State != ConnectionState.Connected || !Client.Connected)
        {
            _logger?.Log(LogLevel.Warning, $"Connection {Id} is not connected, state: {State}");
            return;
        }

        try
        {
            await _stream.WriteAsync(data, ct);
            await _stream.FlushAsync(ct);
            UpdateLastActivity();
        }
        catch (Exception ex)
        {
            _logger?.Log(LogLevel.Error, $"Error sending data to connection {Id}", ex);
            State = ConnectionState.Disconnected;
            throw;
        }
    }

    public void Close()
    {
        if (_disposed || State == ConnectionState.Disconnected) return;
        
        State = ConnectionState.Disconnecting;
        try
        {
            Client.Close();
        }
        catch (Exception ex)
        {
            _logger?.Log(LogLevel.Warning, $"Error closing connection {Id}", ex);
        }
        State = ConnectionState.Disconnected;
        _logger?.Log(LogLevel.Info, $"Connection {Id} closed");
    }

    public void UpdateLastActivity() => LastActivityAt = DateTimeOffset.UtcNow;

    public void Dispose()
    {
        if (_disposed) return;
        
        _logger?.Log(LogLevel.Info, $"Disposing connection {Id}");
        
        Close();
        _stream?.Dispose();
        Client?.Dispose();
        
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
