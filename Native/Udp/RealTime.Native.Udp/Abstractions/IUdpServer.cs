using System.Net;
using RealTime.Native.Common.Models;

namespace RealTime.Native.Udp.Abstractions;

public interface IUdpServer
{
    /// <summary>
    /// Occurs when a client becomes active on the server.
    /// </summary>
    event EventHandler<IPEndPoint> ClientActivity;

    /// <summary>
    /// Occurs when a message is received from a client.
    /// </summary>
    event EventHandler<TransportPackage> MessageReceived;

    /// <summary>
    /// Starts the UDP server on the specified port.
    /// </summary>
    /// <param name="port">The port number to listen on.</param>
    /// <param name="ct">Cancellation token to stop the server.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StartAsync(int port, CancellationToken ct = default);

    /// <summary>
    /// Sends data to the specified target endpoint.
    /// </summary>
    /// <param name="data">The data to send.</param>
    /// <param name="target">The target endpoint to send data to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SendAsync(byte[] data, IPEndPoint target);

    /// <summary>
    /// Stops the UDP server gracefully.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task StopAsync();
}
