namespace RealTime.Native.TcpClient.Core;

public record ClientOptions
{
    // Server manzili va porti
    public string Host { get; init; } = "127.0.0.1";
    public int Port { get; init; } = 5000;

    // Ulanish va kutish sozlamalari
    public int ReceiveBufferSize { get; init; } = 4096;
    public TimeSpan ConnectionTimeout { get; init; } = TimeSpan.FromSeconds(10);

    // Qayta ulanish (Reconnection) strategiyasi
    public bool AutoReconnect { get; init; } = true;
    public int MaxRetryAttempts { get; init; } = 5;
    public TimeSpan ReconnectDelay { get; init; } = TimeSpan.FromSeconds(2);
}
