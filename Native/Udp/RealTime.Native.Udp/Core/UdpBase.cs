using System.Net;
using System.Net.Sockets;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Udp.Common;

namespace RealTime.Native.Udp.Core;

/// <summary>
/// Base class for UDP client and server implementations.
/// </summary>
public abstract class UdpBase
{
    /// <summary>
    /// Gets the underlying UDP client socket.
    /// </summary>
    protected readonly UdpClient Socket;

    /// <summary>
    /// Gets the logger instance for this component.
    /// </summary>
    protected readonly SharedLogger Logger;

    /// <summary>
    /// Gets or sets whether the UDP component is currently running.
    /// </summary>
    protected bool IsRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="UdpBase"/> class.
    /// </summary>
    /// <param name="owner">The owner identifier for logging purposes.</param>
    /// <param name="port">The port to bind to. If null, uses a dynamic port.</param>
    protected UdpBase(string owner, int? port = null)
    {
        Logger = new SharedLogger(owner);
        // If port is provided, bind to it; otherwise use dynamic port (0)
        Socket = port.HasValue ? new UdpClient(port.Value) : new UdpClient(0);

        // Important for UDP: Ignore "Connection Reset" errors on Windows
        if (OperatingSystem.IsWindows())
        {
            try { Socket.Client.IOControl(UdpConstants.SioUdpConnReset, [0], null); }
            catch { /* Permission may be denied in some environments */ }
        }
    }

    /// <summary>
    /// Sends raw data to the specified endpoint.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="endpoint">The endpoint to send data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    protected async Task SendRawAsync(byte[] data, IPEndPoint endpoint)
    {
        try
        {
            await Socket.SendAsync(data, data.Length, endpoint);
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, $"Error sending packet to {endpoint}", ex);
        }
    }
}
