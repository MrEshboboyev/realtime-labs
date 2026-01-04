using System.Net;
using System.Net.Sockets;
using RealTime.Native.Common.Infrastructure;

namespace RealTime.Native.Udp.Core;

public abstract class UdpBase
{
    protected readonly UdpClient Socket;
    protected readonly SharedLogger Logger;
    protected bool IsRunning;

    protected UdpBase(string owner, int? port = null)
    {
        Socket = port.HasValue ? new UdpClient(port.Value) : new UdpClient();
        Logger = new SharedLogger(owner);

        // UDP uchun muhim: "Connection Reset" xatosini e'tiborsiz qoldirish (Windows uchun)
        if (OperatingSystem.IsWindows())
        {
            const int SIO_UDP_CONNRESET = -1744830452;
            Socket.Client.IOControl(SIO_UDP_CONNRESET, [0], null);
        }
    }

    protected async Task SendRawAsync(byte[] data, IPEndPoint endpoint)
    {
        try
        {
            await Socket.SendAsync(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Paket yuborishda xato: {endpoint}", ex);
        }
    }
}
