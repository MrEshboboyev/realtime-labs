namespace RealTime.Native.TcpServer.Models;

/// <summary>
/// Mijozning ulanish holati
/// </summary>
public enum ConnectionState
{
    Connecting,
    Connected,
    Disconnecting,
    Disconnected
}
