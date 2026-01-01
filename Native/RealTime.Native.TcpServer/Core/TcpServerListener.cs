using RealTime.Native.TcpServer.Abstractions;
using RealTime.Native.TcpServer.Infrastructure;
using RealTime.Native.TcpServer.Models;
using RealTime.Native.TcpServer.Protocols.Framing;
using System.Net;
using System.Net.Sockets;

namespace RealTime.Native.TcpServer.Core;

public class TcpServerListener(ServerOptions options) : IServer
{
    private readonly ConnectionManager _connectionManager = new();
    private readonly CancellationTokenSource _cts = new();
    private TcpListener? _listener;
    private readonly Logger _logger = new();
    private readonly BufferPool _bufferPool = new();

    public bool IsRunning { get; private set; }

    public event EventHandler<IConnection>? ClientConnected;
    public event EventHandler<Guid>? ClientDisconnected;
    public event EventHandler<TransportPackage>? MessageReceived;

    public async Task StartAsync(int port, CancellationToken ct = default)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _listener.Start();
        IsRunning = true;

        // 1. Heartbeat monitorini ishga tushirish
        _ = Task.Run(() => MonitorHeartbeatAsync(_cts.Token), _cts.Token);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                var tcpClient = await _listener.AcceptTcpClientAsync(ct);

                // Max connections nazorati
                if (_connectionManager.Count >= options.MaxConnections)
                {
                    tcpClient.Close();
                    continue;
                }

                var connection = new TcpConnection(tcpClient);
                _connectionManager.AddConnection(connection);

                ClientConnected?.Invoke(this, connection);
                _ = Task.Run(() => StartReceiveLoop(connection, _cts.Token), _cts.Token);
            }
            catch (Exception) when (!IsRunning) { break; }
        }
    }

    // O'lik ulanishlarni tozalash (Background Service)
    private async Task MonitorHeartbeatAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            foreach (var conn in _connectionManager.GetAllConnections())
            {
                // Agar mijozdan belgilangan vaqtdan ko'p xabar kelmasa
                if (now - conn.ConnectedAt > options.ClientTimeout)
                {
                    Console.WriteLine($"[TIMEOUT] Mijoz o'chirildi: {conn.Id}");
                    _connectionManager.RemoveConnection(conn.Id);
                    ClientDisconnected?.Invoke(this, conn.Id);
                }
            }
            await Task.Delay(5000, ct); // Har 5 soniyada tekshirish
        }
    }

    private async Task StartReceiveLoop(IConnection connection, CancellationToken ct)
    {
        // Professional yondashuv: Har bir ulanish uchun alohida framing holati
        var frameHandler = new LengthPrefixedFrame();

        byte[] buffer = _bufferPool.Rent();

        try
        {
            var stream = connection.Client.GetStream();

            while (connection.State == ConnectionState.Connected && !ct.IsCancellationRequested)
            {
                // Asinxron o'qish (Timeout va Cancellation'ni hisobga oladi)
                int bytesRead = await stream.ReadAsync(buffer, ct);

                if (bytesRead == 0) break; // Mijoz "Graceful Close" qildi

                // 1. Mijozning faollik vaqtini yangilash (Heartbeat uchun)
                if (connection is TcpConnection tcpConn)
                {
                    tcpConn.UpdateLastActivity();
                }

                // 2. ReadOnlyMemory orqali nusxa ko'chirmasdan ishlov berish
                // ArrayPool'dan olingan bufferning faqat o'qilgan qismini uzatamiz
                var rawSegment = new ArraySegment<byte>(buffer, 0, bytesRead);

                // 3. Framing orqali to'liq xabarlarni ajratish
                // Unwrap ichida faqat kerakli qism olinadi
                var messages = frameHandler.Unwrap(rawSegment.ToArray(), connection.Id);

                foreach (var message in messages)
                {
                    var package = new TransportPackage(connection.Id, message, DateTime.UtcNow);

                    // Eventni xavfsiz chaqirish (Null check va Try-Catch bilan)
                    try
                    {
                        MessageReceived?.Invoke(this, package);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ERROR] Xabarni qayta ishlashda xato: {ex.Message}");
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine($"[INFO] Ulanish to'xtatildi: {connection.Id}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[CRITICAL] Socket xatosi ({connection.Id}): {ex.Message}");
        }
        finally
        {
            // 4. Resurslarni tozalash
            _bufferPool.Return(buffer);

            _connectionManager.RemoveConnection(connection.Id);
            ClientDisconnected?.Invoke(this, connection.Id);

            connection.Dispose();
            Console.WriteLine($"[SERVER] Resurslar tozalandi: {connection.Id}");
        }
    }

    public Task StopAsync()
    {
        IsRunning = false;
        _cts.Cancel();
        _listener?.Stop();
        _connectionManager.Clear();
        return Task.CompletedTask;
    }

    // Tashqaridan ConnectionManagerga xavfsiz kirish uchun
    public ConnectionManager GetConnectionManager() => _connectionManager;
}
