namespace RealTime.Native.Udp.Abstractions;

public interface IUdpClient : IDisposable
{
    event EventHandler<byte[]> MessageReceived;
    event EventHandler<Exception> OnError;

    bool IsActive { get; }
    Task ConnectAsync(string host, int port);
    Task SendAsync<T>(T message);
    Task DisconnectAsync();
}
