namespace RealTime.Native.Udp.Common;

/// <summary>
/// Constants used throughout the UDP communication library.
/// </summary>
public static class UdpConstants
{
    /// <summary>
    /// The maximum size of a UDP packet in bytes.
    /// </summary>
    public const int MaxUdpPacketSize = 65507;

    /// <summary>
    /// The default port for UDP communication.
    /// </summary>
    public const int DefaultPort = 5001;

    /// <summary>
    /// The default timeout for client inactivity in seconds.
    /// </summary>
    public const int DefaultClientTimeoutSeconds = 30;

    /// <summary>
    /// The default heartbeat interval in seconds.
    /// </summary>
    public const int DefaultHeartbeatIntervalSeconds = 5;

    /// <summary>
    /// The default acknowledgment timeout in seconds.
    /// </summary>
    public const int DefaultAckTimeoutSeconds = 10;

    /// <summary>
    /// The default buffer size for UDP packets in bytes.
    /// </summary>
    public const int DefaultBufferSize = 8192;

    /// <summary>
    /// The default maximum retry attempts for failed packets.
    /// </summary>
    public const int DefaultMaxRetryAttempts = 3;

    /// <summary>
    /// The SIO_UDP_CONNRESET constant for Windows UDP socket control.
    /// </summary>
    public const int SioUdpConnReset = -1744830452;
}