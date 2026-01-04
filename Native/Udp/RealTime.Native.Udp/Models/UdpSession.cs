using System.Net;

namespace RealTime.Native.Udp.Models;

public class UdpSession(IPEndPoint endPoint)
{
    public IPEndPoint EndPoint { get; } = endPoint;
    public DateTimeOffset LastActivity { get; private set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset ConnectedAt { get; } = DateTimeOffset.UtcNow;

    public void UpdateActivity() => LastActivity = DateTimeOffset.UtcNow;
}
