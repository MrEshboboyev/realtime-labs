namespace RealTime.Native.TcpClient.Core;

/// <summary>
/// Configuration options for the TCP client
/// </summary>
public record ClientOptions
{
    /// <summary>
    /// Server host address (default: 127.0.0.1)
    /// </summary>
    public string Host { get; init; } = "127.0.0.1";
    
    /// <summary>
    /// Server port (default: 5000)
    /// </summary>
    public int Port { get; init; } = 5000;

    /// <summary>
    /// Size of the receive buffer in bytes (default: 4096)
    /// </summary>
    public int ReceiveBufferSize { get; init; } = 4096;
    
    /// <summary>
    /// Connection timeout duration (default: 10 seconds)
    /// </summary>
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Whether to automatically reconnect on disconnection (default: true)
    /// </summary>
    public bool AutoReconnect { get; init; } = true;
    
    /// <summary>
    /// Maximum number of reconnection attempts (default: 5)
    /// </summary>
    public int MaxRetryAttempts { get; init; } = 5;
    
    /// <summary>
    /// Delay between reconnection attempts (default: 2 seconds)
    /// </summary>
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);
    
    /// <summary>
    /// Validates the client options
    /// </summary>
    /// <returns>True if options are valid, false otherwise</returns>
    public bool Validate()
    {
        return !string.IsNullOrWhiteSpace(Host) &&
               Port > 0 && Port <= 65535 &&
               ReceiveBufferSize > 0 &&
               ConnectionTimeout > TimeSpan.Zero &&
               MaxRetryAttempts >= 0 &&
               ReconnectDelay > TimeSpan.Zero;
    }
}
