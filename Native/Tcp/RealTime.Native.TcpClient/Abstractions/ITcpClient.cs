using RealTime.Native.TcpClient.Events;

namespace RealTime.Native.TcpClient.Abstractions;

/// <summary>
/// Defines the contract for a TCP client implementation
/// </summary>
public interface ITcpClient : IDisposable
{
    /// <summary>
    /// Gets whether the client is currently connected
    /// </summary>
    bool IsConnected { get; }
    
    /// <summary>
    /// Gets the unique identifier for this client
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Connects to the server
    /// </summary>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task ConnectAsync(CancellationToken ct = default);
    
    /// <summary>
    /// Disconnects from the server
    /// </summary>
    /// <returns>A task representing the operation</returns>
    Task DisconnectAsync();

    /// <summary>
    /// Sends a message to the server
    /// </summary>
    /// <typeparam name="T">The type of message to send</typeparam>
    /// <param name="message">The message to send</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>A task representing the operation</returns>
    Task SendAsync<T>(T message, CancellationToken ct = default);

    // Events
    /// <summary>
    /// Event raised when connected to the server
    /// </summary>
    event EventHandler? OnConnected;
    
    /// <summary>
    /// Event raised when disconnected from the server
    /// </summary>
    event EventHandler? OnDisconnected;
    
    /// <summary>
    /// Event raised when a message is received from the server
    /// </summary>
    event EventHandler<OnMessageEventArgs>? OnMessageReceived;
    
    /// <summary>
    /// Event raised when an error occurs
    /// </summary>
    event EventHandler<OnErrorEventArgs>? OnError;
}
