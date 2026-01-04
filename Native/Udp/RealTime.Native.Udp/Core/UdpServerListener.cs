using System.Net;
using System.Net.Sockets;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Udp.Abstractions;

namespace RealTime.Native.Udp.Core;

public class UdpServerListener(
    TimeSpan clientTimeout
) : IUdpServer
{
    private UdpClient? _udpClient;
    private readonly SessionManager _sessionManager = new(clientTimeout);
    private readonly SharedLogger _logger = new("UDP-SERVER");
    private readonly CancellationTokenSource _cts = new();

    public event EventHandler<IPEndPoint>? ClientActivity;
    public event EventHandler<TransportPackage>? MessageReceived;

    public async Task StartAsync(int port, CancellationToken ct = default)
    {
        _udpClient = new UdpClient(port);
        _logger.Log(LogLevel.Success, $"UDP Server {port}-portda tinglamoqda...");

        // Fon rejimida o'lik sessiyalarni tozalab turish
        _ = CleanupLoop(_cts.Token);

        try
        {
            while (!ct.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(ct);

                // 1. Sessiyani yangilash yoki yaratish
                var session = _sessionManager.GetOrAdd(result.RemoteEndPoint);
                ClientActivity?.Invoke(this, result.RemoteEndPoint);

                // 2. Paketni TransportPackage ko'rinishiga keltirish
                // UDPda ConnectionId o'rniga EndPointning hashini ishlatamiz
                var package = new TransportPackage(
                    Guid.Empty, // UDPda qat'iy GUID yo'q
                    result.Buffer,
                    DateTimeOffset.UtcNow,
                    result.RemoteEndPoint
                );

                MessageReceived?.Invoke(this, package);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, "UDP qabul qilishda xato", ex);
        }
    }

    public async Task SendAsync(byte[] data, IPEndPoint target)
    {
        if (_udpClient == null) return;
        await _udpClient.SendAsync(data, data.Length, target);
    }

    private async Task CleanupLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            _ = _sessionManager.GetActiveSessions().Count();
            await Task.Delay(10000, ct); // Har 10 soniyada tozalash
        }
    }

    public Task StopAsync()
    {
        _cts.Cancel();
        _udpClient?.Close();
        return Task.CompletedTask;
    }
}
