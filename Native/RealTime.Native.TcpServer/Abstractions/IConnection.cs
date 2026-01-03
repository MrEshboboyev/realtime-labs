using RealTime.Native.Common.Models;
using System.Net.Sockets;

namespace RealTime.Native.TcpServer.Abstractions;

/// <summary>
/// Defines the contract for a TCP connection
/// </summary>
public interface IConnection : IDisposable
{
    /// <summary>
    /// Gets the unique identifier for this connection
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Gets the underlying TCP client
    /// </summary>
    TcpClient Client { get; }
    
    /// <summary>
    /// Gets the current connection state
    /// </summary>
    ConnectionState State { get; }
    
    /// <summary>
    /// Gets the time when the connection was established
    /// </summary>
    DateTimeOffset ConnectedAt { get; }
    
    /// <summary>
    /// Gets or sets the time of last activity on this connection
    /// </summary>
    DateTimeOffset LastActivityAt { get; set; }

    /// <summary>
    /// Sends data asynchronously
    /// </summary>
    /// <param name="data">The data to send</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task SendAsync(ReadOnlyMemory<byte> data, CancellationToken ct = default);

    /// <summary>
    /// Closes the connection
    /// </summary>
    void Close();

    /// <summary>
    /// Updates the last activity time to the current time
    /// </summary>
    void UpdateLastActivity();
}
