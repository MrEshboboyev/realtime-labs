using System.Net;
using RealTime.Native.Common.Models;

namespace RealTime.Native.Udp.Abstractions;

public interface IUdpServer
{
    event EventHandler<IPEndPoint> ClientActivity;
    event EventHandler<TransportPackage> MessageReceived;
    Task StartAsync(int port, CancellationToken ct = default);
    Task SendAsync(byte[] data, IPEndPoint target);
    Task StopAsync();
}
