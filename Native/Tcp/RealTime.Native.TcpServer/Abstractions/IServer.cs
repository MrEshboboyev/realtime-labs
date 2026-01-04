using RealTime.Native.Common.Models;

namespace RealTime.Native.TcpServer.Abstractions;

/// <summary>
/// Defines the contract for a TCP server implementation
/// </summary>
public interface IServer
{
    /// <summary>
    /// Gets whether the server is currently running
    /// </summary>
    bool IsRunning { get; }
    
    /// <summary>
    /// Starts the server on the specified port
    /// </summary>
    /// <param name="port">The port to listen on</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task StartAsync(int port, CancellationToken ct = default);
    
    /// <summary>
    /// Stops the server
    /// </summary>
    /// <returns>A task representing the operation</returns>
    Task StopAsync();

    // Events
    /// <summary>
    /// Event raised when a client connects
    /// </summary>
    event EventHandler<IConnection> ClientConnected;
    
    /// <summary>
    /// Event raised when a client disconnects
    /// </summary>
    event EventHandler<Guid> ClientDisconnected;
    
    /// <summary>
    /// Event raised when a message is received
    /// </summary>
    event EventHandler<TransportPackage> MessageReceived;
}
