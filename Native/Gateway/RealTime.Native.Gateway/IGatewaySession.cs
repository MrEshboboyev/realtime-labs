using System.Net;

namespace RealTime.Native.Gateway;

public interface IGatewaySession
{
    Guid SessionId { get; }
    string UserName { get; set; }
    IPEndPoint? UdpEndPoint { get; set; }
    DateTimeOffset LastActivity { get; }

    Task SendAsync<T>(T message, bool reliable = false);
    void UpdateActivity();
}
