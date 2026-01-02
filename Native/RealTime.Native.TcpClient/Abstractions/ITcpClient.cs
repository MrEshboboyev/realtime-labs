using RealTime.Native.TcpClient.Events;

namespace RealTime.Native.TcpClient.Abstractions;

public interface ITcpClient : IDisposable
{
    // Holatlar
    bool IsConnected { get; }
    Guid Id { get; }

    // Amallar
    Task ConnectAsync(CancellationToken ct = default);
    Task DisconnectAsync();

    // Ma'lumot yuborish (Generic - har qanday turdagi obyektni yubora olish uchun)
    Task SendAsync<T>(T message, CancellationToken ct = default);

    // Voqealar
    event EventHandler? OnConnected;
    event EventHandler? OnDisconnected;
    event EventHandler<OnMessageEventArgs>? OnMessageReceived;
    event EventHandler<OnErrorEventArgs>? OnError;
}
