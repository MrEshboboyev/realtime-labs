using RealTime.Native.Udp.Abstractions;
using RealTime.Native.Udp.Configuration;
using RealTime.Native.Udp.Core;

namespace RealTime.Native.Udp.Factories;

/// <summary>
/// Factory for creating UDP client and server instances with proper configuration.
/// </summary>
public static class UdpFactory
{
    /// <summary>
    /// Creates a new UDP client with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use for the client.</param>
    /// <returns>A configured UDP client instance.</returns>
    public static IUdpClient CreateClient(UdpConfiguration configuration)
    {
        return new NativeUdpClient();
    }

    /// <summary>
    /// Creates a new UDP server with the specified configuration.
    /// </summary>
    /// <param name="configuration">The configuration to use for the server.</param>
    /// <returns>A configured UDP server instance.</returns>
    public static IUdpServer CreateServer(UdpConfiguration configuration)
    {
        return new UdpServerListener(configuration.ClientTimeout);
    }

    /// <summary>
    /// Creates a default UDP client with default configuration.
    /// </summary>
    /// <returns>A UDP client instance with default settings.</returns>
    public static IUdpClient CreateDefaultClient()
    {
        return CreateClient(new UdpConfiguration());
    }

    /// <summary>
    /// Creates a default UDP server with default configuration.
    /// </summary>
    /// <returns>A UDP server instance with default settings.</returns>
    public static IUdpServer CreateDefaultServer()
    {
        return CreateServer(new UdpConfiguration());
    }
}
