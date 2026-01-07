using System.ComponentModel.DataAnnotations;

namespace RealTime.Native.Udp.Configuration;

/// <summary>
/// Configuration settings for UDP communication.
/// </summary>
public class UdpConfiguration
{
    /// <summary>
    /// Gets or sets the default port for UDP communication.
    /// </summary>
    [Range(1, 65535)]
    public int Port { get; set; } = 5001;

    /// <summary>
    /// Gets or sets the timeout for client inactivity before session cleanup.
    /// </summary>
    public TimeSpan ClientTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the heartbeat interval for maintaining connections.
    /// </summary>
    public TimeSpan HeartbeatInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Gets or sets the timeout for acknowledgments in reliable communication.
    /// </summary>
    public TimeSpan AckTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets the buffer size for UDP packets.
    /// </summary>
    [Range(1024, 65507)] // Max UDP packet size is 65507 bytes
    public int BufferSize { get; set; } = 8192;

    /// <summary>
    /// Gets or sets whether to enable reliable communication features.
    /// </summary>
    public bool EnableReliability { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum retry attempts for failed packets.
    /// </summary>
    [Range(1, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
}
