using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.TcpServer.Abstractions;
using System.Net;
using System.Net.Sockets;

namespace RealTime.Native.TcpServer.Core;

public class TcpServerListener(
    ServerOptions options
) : IServer
{
    private readonly ServerOptions _options = options;
    private readonly ConnectionManager _connectionManager = new();
    private readonly CancellationTokenSource _cts = new();
    private TcpListener? _listener;

    private readonly SharedLogger _logger = new("SERVER");
    private readonly NetworkBufferPool _bufferPool = NetworkBufferPool.Shared;

    public bool IsRunning { get; private set; }

    // Eventlar
    public event EventHandler<IConnection>? ClientConnected;
    public event EventHandler<Guid>? ClientDisconnected;
    public event EventHandler<TransportPackage>? MessageReceived;

    public async Task StartAsync(int port, CancellationToken ct = default)
    {
        try
        {
            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();
            IsRunning = true;

            _logger.Log(LogLevel.Success, $"Server {port}-portda tinglashni boshladi.");

            // Background monitorlarni ishga tushirish
            _ = Task.Run(() => MonitorHeartbeatAsync(_cts.Token), _cts.Token);

            while (!ct.IsCancellationRequested)
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);

                if (_connectionManager.Count >= _options.MaxConnections)
                {
                    _logger.Log(LogLevel.Warning, "Maksimal ulanishlar soniga yetildi. Yangi ulanish rad etildi.");
                    tcpClient.Close();
                    continue;
                }

                var connection = new TcpConnection(tcpClient);
                _connectionManager.AddConnection(connection);

                ClientConnected?.Invoke(this, connection);

                // Har bir mijoz uchun alohida Receive Loop
                _ = Task.Run(() => StartReceiveLoop(connection, _cts.Token), _cts.Token);
            }
        }
        catch (Exception ex) when (IsRunning)
        {
            _logger.Log(LogLevel.Critical, "Server ishlashida jiddiy xato!", ex);
        }
        finally
        {
            await StopAsync();
        }
    }

    private async Task StartReceiveLoop(IConnection connection, CancellationToken ct)
    {
        var frameHandler = new LengthPrefixedFrame();
        byte[] buffer = _bufferPool.Rent(_options.ReceiveBufferSize);

        try
        {
            var stream = connection.Client.GetStream();
            _logger.Log(LogLevel.Info, $"Mijoz uchun ReceiveLoop boshlandi: {connection.Id}");

            while (connection.State == ConnectionState.Connected && !ct.IsCancellationRequested)
            {
                // stream.ReadAsync ga buffer, offset va count berish xavfsizroq
                int bytesRead = await stream.ReadAsync(buffer, ct);

                if (bytesRead == 0) break;

                // Heartbeat vaqtini yangilash
                if (connection is TcpConnection tcpConn)
                    tcpConn.UpdateLastActivity();

                // Bufferdan faqat o'qilgan qismini olish (Nusxa ko'chirmasdan)
                var receivedSegment = new ArraySegment<byte>(buffer, 0, bytesRead);

                // Framing mantiqi
                var messages = frameHandler.Unwrap(receivedSegment.ToArray(), connection.Id);

                foreach (var message in messages)
                {
                    var package = new TransportPackage(connection.Id, message, DateTime.UtcNow);

                    try
                    {
                        MessageReceived?.Invoke(this, package);
                    }
                    catch (Exception ex)
                    {
                        _logger.Log(LogLevel.Error, $"MessageReceived eventida xatolik ({connection.Id})", ex);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.Log(LogLevel.Error, $"Mijoz ulanishida xatolik: {connection.Id}", ex);
        }
        finally
        {
            _bufferPool.Return(buffer);
            _connectionManager.RemoveConnection(connection.Id);
            ClientDisconnected?.Invoke(this, connection.Id);

            connection.Dispose();
            _logger.Log(LogLevel.Info, $"Mijoz resurslari tozalandi: {connection.Id}");
        }
    }

    private async Task MonitorHeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var now = DateTime.UtcNow;
                foreach (var conn in _connectionManager.GetAllConnections())
                {
                    if (conn is TcpConnection tcpConn && (now - tcpConn.LastActivityAt) > _options.ClientTimeout)
                    {
                        _logger.Log(LogLevel.Warning, $"[TIMEOUT] Mijoz faol emas: {conn.Id}");
                        tcpConn.Dispose(); // Bu StartReceiveLoop dagi loopni buzadi
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Log(LogLevel.Error, "Heartbeat monitorda xatolik", ex);
            }
            await Task.Delay(5000, ct);
        }
    }

    public Task StopAsync()
    {
        if (!IsRunning) return Task.CompletedTask;

        IsRunning = false;
        _cts.Cancel();
        _listener?.Stop();
        _connectionManager.Clear();
        _logger.Log(LogLevel.Critical, "Server to'xtatildi.");

        return Task.CompletedTask;
    }

    public ConnectionManager GetConnectionManager() => _connectionManager;
}
