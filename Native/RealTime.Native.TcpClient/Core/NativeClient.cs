using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpClient.Abstractions;
using RealTime.Native.TcpClient.Events;
using System.Buffers;
using System.Net.Sockets;

namespace RealTime.Native.TcpClient.Core;

public class NativeClient : ITcpClient
{
    private System.Net.Sockets.TcpClient? _client;
    private NetworkStream? _stream;
    private readonly ClientOptions _options;
    private readonly IFrameHandler _frameHandler;
    private readonly ISerializer _serializer;
    private CancellationTokenSource? _cts;
    private readonly ReconnectionManager _reconnectionManager;
    private readonly SharedLogger _logger = new("CLIENT");

    public Guid Id { get; private set; } = Guid.NewGuid();
    public bool IsConnected => _client?.Connected ?? false;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    // Voqealar
    public event EventHandler? OnConnected;
    public event EventHandler? OnDisconnected;
    public event EventHandler<OnMessageEventArgs>? OnMessageReceived;
    public event EventHandler<OnErrorEventArgs>? OnError;

    public NativeClient(ClientOptions options)
    {
        _options = options;
        _frameHandler = new LengthPrefixedFrame();
        _serializer = new BinarySerializer();

        // Manager o'zini eventga bog'lab oladi
        _reconnectionManager = new ReconnectionManager(this, _options, _logger);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (IsConnected) return;

        try
        {
            _client = new System.Net.Sockets.TcpClient();
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // 1. Serverga ulanish
            await _client.ConnectAsync(_options.Host, _options.Port, _cts.Token);
            _stream = _client.GetStream();
            State = ConnectionState.Connected;

            OnConnected?.Invoke(this, EventArgs.Empty);

            // 2. Xabarlarni qabul qilish tsiklini (Background Loop) boshlash
            _ = Task.Run(() => StartReceiveLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            State = ConnectionState.Disconnected;
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "Connect"));
            throw;
        }
    }

    private async Task StartReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);

        try
        {
            while (!ct.IsCancellationRequested && IsConnected)
            {
                int bytesRead = await _stream!.ReadAsync(buffer, 0, buffer.Length, ct);
                if (bytesRead == 0) break;

                // Framing orqali xabarlarni ajratish
                var receivedSegment = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, receivedSegment, 0, bytesRead);

                var frames = _frameHandler.Unwrap(receivedSegment, Id);

                foreach (var frame in frames)
                {
                    OnMessageReceived?.Invoke(this, new OnMessageEventArgs(frame));
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested)
        {
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "ReceiveLoop"));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
            await DisconnectAsync();
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken ct = default)
    {
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("Serverga ulanmagan!");

        try
        {
            // Serialize -> Wrap (Frame) -> Send
            byte[] data = _serializer.Serialize(message);
            byte[] framedData = _frameHandler.Wrap(data);

            await _stream.WriteAsync(framedData, ct);
            await _stream.FlushAsync(ct);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "Send"));
            throw;
        }
    }

    public Task DisconnectAsync()
    {
        if (State == ConnectionState.Disconnected) return Task.CompletedTask;

        State = ConnectionState.Disconnected;
        _cts?.Cancel();
        _stream?.Dispose();
        _client?.Close();

        OnDisconnected?.Invoke(this, EventArgs.Empty);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _client?.Dispose();

        GC.SuppressFinalize(this);
    }
}
