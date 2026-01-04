using System.Collections.Concurrent;
using System.Net;
using RealTime.Native.Udp.Models;

namespace RealTime.Native.Udp.Core;

public class SessionManager(TimeSpan timeout)
{
    private readonly ConcurrentDictionary<IPEndPoint, UdpSession> _sessions = new();

    public UdpSession GetOrAdd(IPEndPoint endPoint)
    {
        var session = _sessions.GetOrAdd(endPoint, ep => new UdpSession(ep));
        session.UpdateActivity();
        return session;
    }

    public IEnumerable<UdpSession> GetActiveSessions()
    {
        var now = DateTime.UtcNow;
        foreach (var session in _sessions.Values)
        {
            if (now - session.LastActivity <= timeout)
                yield return session;
            else
                _sessions.TryRemove(session.EndPoint, out _);
        }
    }

    public void Remove(IPEndPoint endPoint) => _sessions.TryRemove(endPoint, out _);
}
