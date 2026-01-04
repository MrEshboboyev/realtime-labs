namespace RealTime.Native.TcpServer.Core;

/// <summary>
/// Configuration options for the TCP server
/// </summary>
public record ServerOptions
{
    /// <summary>
    /// The port number to listen on (default: 5000)
    /// </summary>
    public int Port { get; init; } = 5000;
    
    /// <summary>
    /// Size of the receive buffer in bytes (default: 4096)
    /// </summary>
    public int ReceiveBufferSize { get; init; } = 4096;
    
    /// <summary>
    /// Timeout for client inactivity (default: 60 seconds)
    /// </summary>
    public TimeSpan ClientTimeout { get; init; } = TimeSpan.FromSeconds(60);
    
    /// <summary>
    /// Maximum number of simultaneous connections (default: 1000)
    /// </summary>
    public int MaxConnections { get; init; } = 1000;
    
    /// <summary>
    /// Whether to enable TCP keep-alive (default: true)
    /// </summary>
    public bool UseKeepAlive { get; init; } = true;
    
    /// <summary>
    /// Validates the server options
    /// </summary>
    /// <returns>True if options are valid, false otherwise</returns>
    public bool Validate()
    {
        return Port > 0 && Port <= 65535 && 
               ReceiveBufferSize > 0 && 
               MaxConnections > 0 &&
               ClientTimeout > TimeSpan.Zero;
    }
}
