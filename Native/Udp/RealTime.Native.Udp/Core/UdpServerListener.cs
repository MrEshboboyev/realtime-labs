using System.Net;
using System.Net.Sockets;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Udp.Abstractions;

namespace RealTime.Native.Udp.Core;

/// <summary>
/// Implements a UDP server listener that manages client connections and messages.
/// </summary>
public class UdpServerListener(
    TimeSpan clientTimeout
) : IUdpServer
{
    private UdpClient? _udpClient;
    private readonly SessionManager _sessionManager = new(clientTimeout);
    private readonly SharedLogger _logger = new("UDP-SERVER");
    private readonly CancellationTokenSource _cancellationTokenSource = new();

    /// <summary>
    /// Occurs when a client becomes active on the server.
    /// </summary>
    public event EventHandler<IPEndPoint>? ClientActivity;

    /// <summary>
    /// Occurs when a message is received from a client.
    /// </summary>
    public event EventHandler<TransportPackage>? MessageReceived;

    /// <summary>
    /// Starts the UDP server on the specified port.
    /// </summary>
    /// <param name="port">The port number to listen on.</param>
    /// <param name="cancellationToken">Cancellation token to stop the server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task StartAsync(int port, CancellationToken cancellationToken = default)
    {
        _udpClient = new UdpClient(port);
        _logger.Log(LogLevel.Success, $"UDP Server listening on port {port}...");

        // Background cleanup of inactive sessions
        _ = CleanupLoop(_cancellationTokenSource.Token);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var result = await _udpClient.ReceiveAsync(cancellationToken);

                // 1. Update or create session
                var session = _sessionManager.GetOrAdd(result.RemoteEndPoint);
                ClientActivity?.Invoke(this, result.RemoteEndPoint);

                // 2. Convert packet to TransportPackage
                // Using endpoint hash instead of ConnectionId in UDP
                var package = new TransportPackage(
                    Guid.Empty, // No fixed GUID in UDP
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
            _logger.Log(LogLevel.Error, "UDP reception error", ex);
        }
    }

    /// <summary>
    /// Sends data to the specified target endpoint.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="target">The target endpoint to send data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAsync(byte[] data, IPEndPoint target)
    {
        if (_udpClient == null) return;
        await _udpClient.SendAsync(data, data.Length, target);
    }

    /// <summary>
    /// Runs the cleanup loop to remove inactive sessions.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task CleanupLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            _ = _sessionManager.GetActiveSessions().Count();
            await Task.Delay(10000, cancellationToken); // Clean up every 10 seconds
        }
    }

    /// <summary>
    /// Stops the UDP server gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task StopAsync()
    {
        _cancellationTokenSource.Cancel();
        _udpClient?.Close();
        return Task.CompletedTask;
    }
}
