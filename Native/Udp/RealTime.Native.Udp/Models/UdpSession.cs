using System.Net;

namespace RealTime.Native.Udp.Models;

/// <summary>
/// Represents a UDP session with an endpoint.
/// </summary>
public class UdpSession(IPEndPoint endPoint)
{
    /// <summary>
    /// Gets the endpoint associated with this session.
    /// </summary>
    public IPEndPoint EndPoint { get; } = endPoint;

    /// <summary>
    /// Gets the last activity timestamp for this session.
    /// </summary>
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Gets the timestamp when this session was connected.
    /// </summary>
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Updates the last activity timestamp to the current time.
    /// </summary>
    public void UpdateActivity() => LastActivity = DateTimeOffset.UtcNow;
}
