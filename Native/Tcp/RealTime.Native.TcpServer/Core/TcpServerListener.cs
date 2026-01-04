using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.TcpServer.Abstractions;
using System.Net;
using System.Net.Sockets;
using RealTime.Native.Common.Protocols.Serialization;

namespace RealTime.Native.TcpServer.Core;

/// <summary>
/// Implements the TCP server that listens for and handles client connections
/// </summary>
public class TcpServerListener : IServer
{
    private readonly ServerOptions _options;
    private readonly ConnectionManager _connectionManager;
    private readonly IFrameHandler _frameHandler;
    private readonly ISerializer _serializer;
    private readonly SharedLogger _logger;
    private readonly NetworkBufferPool _bufferPool = NetworkBufferPool.Shared;
    
    private readonly CancellationTokenSource _cts = new();
    private TcpListener? _listener;
    
    public bool IsRunning { get; private set; }

    // Events
    public event EventHandler<IConnection>? ClientConnected;
    public event EventHandler<Guid>? ClientDisconnected;
    public event EventHandler<TransportPackage>? MessageReceived;

    public TcpServerListener(ServerOptions options, ConnectionManager? connectionManager = null, IFrameHandler? frameHandler = null, ISerializer? serializer = null, SharedLogger? logger = null)
    {
        _options = options;
        _connectionManager = connectionManager ?? new ConnectionManager(logger);
        _frameHandler = frameHandler ?? new LengthPrefixedFrame(logger);
        _serializer = serializer ?? new BinarySerializer(logger);
        _logger = logger ?? new SharedLogger("SERVER");
        
        if (!_options.Validate())
        {
            throw new ArgumentException("Invalid server options provided");
        }
    }

    public async Task StartAsync(int port, CancellationToken ct = default)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;

            _logger.Log(LogLevel.Success, $"Server started listening on port {port}.");

            // Start background monitors
            _ = Task.Run(() => MonitorHeartbeatAsync(_cts.Token), _cts.Token);

            while (!ct.IsCancellationRequested && IsRunning)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);

                if (_connectionManager.Count >= _options.MaxConnections)
                {
                    _logger.Log(LogLevel.Warning, "Maximum connections reached. New connection rejected.");
                    tcpClient.Close();
                    continue;
                }

                var connection = new TcpConnection(tcpClient, _logger);
                _connectionManager.AddConnection(connection);

                ClientConnected?.Invoke(this, connection);

                // Start a separate receive loop for each client
                _ = Task.Run(() => StartReceiveLoop(connection, _cts.Token), _cts.Token);
            }
        }
        catch (ObjectDisposedException)
        {
            // Expected when server is stopped
            _logger.Log(LogLevel.Info, "Server listener disposed");
        }
        catch (Exception ex) when (IsRunning)
        {
            _logger.Log(LogLevel.Critical, "Critical server error!", ex);
            throw;
        }
        finally
        {
            await StopAsync();
        }
    }

    private async Task StartReceiveLoop(IConnection connection, CancellationToken ct)
    {
        byte[] buffer = _bufferPool.Rent(_options.ReceiveBufferSize);

        try
        {
            var stream = connection.Client.GetStream();
            _logger.Log(LogLevel.Info, $"ReceiveLoop started for client: {connection.Id}");

            while (connection.State == ConnectionState.Connected && !ct.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, _options.ReceiveBufferSize, ct);

                if (bytesRead == 0) break;

                // Update heartbeat time
                if (connection is TcpConnection tcpConn)
                    tcpConn.UpdateLastActivity();

                // Get only the read portion of the buffer
                var receivedSegment = new ArraySegment<byte>(buffer, 0, bytesRead);

                // Framing logic
                var messages = _frameHandler.Unwrap(receivedSegment.ToArray(), connection.Id);

                foreach (var message in messages)
                {
                    var package = new TransportPackage(connection.Id, message, DateTimeOffset.UtcNow);

                    try
                    {
                        MessageReceived?.Invoke(this, package);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, $"Error in MessageReceived event ({connection.Id})", ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Log(LogLevel.Error, $"Client connection error: {connection.Id}", ex);
        }
        finally
        {
            _bufferPool.Return(buffer, true); // Clear buffer to prevent data leaks
            _connectionManager.RemoveConnection(connection.Id);
            ClientDisconnected?.Invoke(this, connection.Id);

            connection.Dispose();
            _logger.Log(LogLevel.Info, $"Client resources cleaned up: {connection.Id}");
        }
    }

    private async Task MonitorHeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTimeOffset.UtcNow;
                foreach (var conn in _connectionManager.GetAllConnections())
                {
                    if (conn is TcpConnection tcpConn && (now - tcpConn.LastActivityAt) > _options.ClientTimeout)
                    {
                        _logger.Log(LogLevel.Warning, $"[TIMEOUT] Client inactive: {conn.Id}");
                        tcpConn.Dispose(); // This breaks the loop in StartReceiveLoop
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Heartbeat monitor error", ex);
            }
            
            try
            {
                await Task.Delay(5000, ct); // 5 second heartbeat check
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
                break;
            }
        }
    }

    public async Task StopAsync()
    {
        if (!IsRunning) return;

        IsRunning = false;
        _cts.Cancel();
        _listener?.Stop();
        _connectionManager.Clear();
        _logger.Log(LogLevel.Critical, "Server stopped.");

        await Task.CompletedTask;
    }

    public ConnectionManager GetConnectionManager() => _connectionManager;
    
    /// <summary>
    /// Broadcasts a message to all connected clients
    /// </summary>
    /// <typeparam name="T">The type of message to send</typeparam>
    /// <param name="message">The message to broadcast</param>
    /// <param name="ct">Cancellation token</param>
    public async Task BroadcastAsync<T>(T message, CancellationToken ct = default)
    {
        if (!IsRunning) return;
        
        try
        {
            byte[] serializedData = _serializer.Serialize(message);
            byte[] framedData = _frameHandler.Wrap(serializedData);
            
            var tasks = _connectionManager.GetAllConnections()
                .Select(conn => conn.SendAsync(framedData, ct))
                .ToList();
            
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "Error broadcasting message", ex);
        }
    }
    
    /// <summary>
    /// Sends a message to all clients in a specific room
    /// </summary>
    /// <typeparam name="T">The type of message to send</typeparam>
    /// <param name="roomId">The room ID</param>
    /// <param name="message">The message to send</param>
    /// <param name="ct">Cancellation token</param>
    public async Task SendToRoomAsync<T>(string roomId, T message, CancellationToken ct = default)
    {
        if (!IsRunning) return;
        
        try
        {
            byte[] serializedData = _serializer.Serialize(message);
            byte[] framedData = _frameHandler.Wrap(serializedData);
            
            var tasks = _connectionManager.GetRoomClients(roomId)
                .Select(conn => conn.SendAsync(framedData, ct))
                .ToList();
            
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"Error sending message to room {roomId}", ex);
        }
    }
}
