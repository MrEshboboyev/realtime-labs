using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpClient.Abstractions;
using RealTime.Native.TcpClient.Events;
using System.Buffers;
using System.Net.Sockets;

namespace RealTime.Native.TcpClient.Core;

/// <summary>
/// Implements a TCP client for connecting to the server
/// </summary>
public class NativeTcpClient : ITcpClient
{
    private System.Net.Sockets.TcpClient? _client;
    private NetworkStream? _stream;
    private readonly ClientOptions _options;
    private readonly IFrameHandler _frameHandler;
    private readonly ISerializer _serializer;
    private CancellationTokenSource? _cts;
    private readonly ReconnectionManager _reconnectionManager;
    private readonly SharedLogger _logger;
    
    private readonly Lock _stateLock = new();
    private bool _disposed = false;

    public Guid Id { get; private set; } = Guid.NewGuid();
    public bool IsConnected => _client?.Connected ?? false;
    public ConnectionState State { get; private set; } = ConnectionState.Disconnected;

    // Events
    public event EventHandler? OnConnected;
    public event EventHandler? OnDisconnected;
    public event EventHandler<OnMessageEventArgs>? OnMessageReceived;
    public event EventHandler<OnErrorEventArgs>? OnError;

    public NativeTcpClient(ClientOptions options, IFrameHandler? frameHandler = null, ISerializer? serializer = null, SharedLogger? logger = null)
    {
        _options = options;
        _frameHandler = frameHandler ?? new LengthPrefixedFrame(logger);
        _serializer = serializer ?? new BinarySerializer(logger);
        _logger = logger ?? new SharedLogger("CLIENT");
        
        if (!_options.Validate())
        {
            throw new ArgumentException("Invalid client options provided");
        }

        // Manager subscribes to its own events
        _reconnectionManager = new ReconnectionManager(this, _options, _logger);
    }

    public async Task ConnectAsync(CancellationToken ct = default)
    {
        if (_disposed)
        {
            _logger.Log(LogLevel.Warning, "Attempted to connect on disposed client");
            return;
        }
        
        lock (_stateLock)
        {
            if (IsConnected) return;
        }

        try
        {
            _client = new System.Net.Sockets.TcpClient();
            _client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
            _cts = CancellationTokenSource.CreateLinkedTokenSource(ct);

            // Connect to server with timeout
            using var connectCts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, ct);
            using var delayCts = new CancellationTokenSource(_options.ConnectionTimeout);
            
            var connectTask = Task.Run(() => _client.Connect(_options.Host, _options.Port));
            var timeoutTask = Task.Delay(_options.ConnectionTimeout, delayCts.Token);
            
            var completedTask = await Task.WhenAny(connectTask, timeoutTask);
            
            if (completedTask == timeoutTask)
            {
                throw new TimeoutException($"Connection to {_options.Host}:{_options.Port} timed out after {_options.ConnectionTimeout}");
            }
            
            // If timeout task completed, it means the connection task didn't complete in time
            if (delayCts.IsCancellationRequested && !connectTask.IsCompleted)
            {
                throw new TimeoutException($"Connection to {_options.Host}:{_options.Port} timed out after {_options.ConnectionTimeout}");
            }
            
            await connectTask; // This will throw if the connection failed
            
            _stream = _client.GetStream();
            
            lock (_stateLock)
            {
                State = ConnectionState.Connected;
            }

            OnConnected?.Invoke(this, EventArgs.Empty);

            // Start the message receive loop
            _ = Task.Run(() => StartReceiveLoopAsync(_cts.Token), _cts.Token);
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                State = ConnectionState.Disconnected;
            }
            _logger.Log(LogLevel.Error, "Connection failed", ex);
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "Connect"));
            throw;
        }
    }

    private async Task StartReceiveLoopAsync(CancellationToken ct)
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(_options.ReceiveBufferSize);

        try
        {
            while (!ct.IsCancellationRequested && IsConnected && !_disposed)
            {
                int bytesRead = await _stream!.ReadAsync(buffer, 0, _options.ReceiveBufferSize, ct);
                if (bytesRead == 0) break;

                // Get only the read portion of the buffer
                var receivedSegment = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, receivedSegment, 0, bytesRead);

                var frames = _frameHandler.Unwrap(receivedSegment, Id);

                foreach (var frame in frames)
                {
                    OnMessageReceived?.Invoke(this, new OnMessageEventArgs(frame));
                }
            }
        }
        catch (Exception ex) when (!ct.IsCancellationRequested && !_disposed)
        {
            _logger.Log(LogLevel.Error, "Error in receive loop", ex);
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "ReceiveLoop"));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, true); // Clear buffer to prevent data leaks
            if (!_disposed)
            {
                await DisconnectAsync();
            }
        }
    }

    public async Task SendAsync<T>(T message, CancellationToken ct = default)
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(NativeTcpClient));
        }
        
        if (!IsConnected || _stream == null)
            throw new InvalidOperationException("Not connected to server!");

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
            _logger.Log(LogLevel.Error, "Error sending message", ex);
            OnError?.Invoke(this, new OnErrorEventArgs(ex, "Send"));
            throw;
        }
    }

    public async Task DisconnectAsync()
    {
        if (_disposed) return;
        
        lock (_stateLock)
        {
            if (State == ConnectionState.Disconnected) return;
            State = ConnectionState.Disconnecting;
        }

        _cts?.Cancel();
        _stream?.Dispose();
        _client?.Close();
        
        lock (_stateLock)
        {
            State = ConnectionState.Disconnected;
        }

        OnDisconnected?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        _disposed = true;
        
        DisconnectAsync().GetAwaiter().GetResult();
        _cts?.Dispose();
        _client?.Dispose();

        GC.SuppressFinalize(this);
    }
}
