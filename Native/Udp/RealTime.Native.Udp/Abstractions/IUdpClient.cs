namespace RealTime.Native.Udp.Abstractions;

public interface IUdpClient : IDisposable
{
    /// <summary>
    /// Occurs when a message is received from the remote endpoint.
    /// </summary>
    event EventHandler<byte[]> MessageReceived;

    /// <summary>
    /// Occurs when an error occurs during UDP communication.
    /// </summary>
    event EventHandler<Exception> OnError;

    /// <summary>
    /// Gets whether the client is currently active and connected.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Connects the UDP client to the specified host and port.
    /// </summary>
    /// <param name="host">The host address to connect to.</param>
    /// <param name="port">The port number to connect to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConnectAsync(string host, int port);

    /// <summary>
    /// Sends a message to the connected endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync<T>(T message);

    /// <summary>
    /// Disconnects the UDP client from the remote endpoint.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task DisconnectAsync();
}
