namespace RealTime.Native.TcpServer.Core;

public record ServerOptions
{
    public int Port { get; init; } = 5000;
    public int ReceiveBufferSize { get; init; } = 4096;
    public TimeSpan ClientTimeout { get; init; } = TimeSpan.FromSeconds(60);
    public int MaxConnections { get; init; } = 1000;
    public bool UseKeepAlive { get; init; } = true;
}
