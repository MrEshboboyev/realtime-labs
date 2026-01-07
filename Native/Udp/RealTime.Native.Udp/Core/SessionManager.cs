using System.Collections.Concurrent;
using System.Net;
using RealTime.Native.Udp.Models;

namespace RealTime.Native.Udp.Core;

/// <summary>
/// Manages UDP sessions and tracks client activity.
/// </summary>
public class SessionManager(TimeSpan timeout)
{
    private readonly ConcurrentDictionary<IPEndPoint, UdpSession> _sessions = new();

    /// <summary>
    /// Gets an existing session or creates a new one for the given endpoint.
    /// </summary>
    /// <param name="endPoint">The endpoint to get or create a session for.</param>
    /// <returns>The existing or newly created session.</returns>
    public UdpSession GetOrAdd(IPEndPoint endPoint)
    {
        var session = _sessions.GetOrAdd(endPoint, ep => new UdpSession(ep));
        session.UpdateActivity();
        return session;
    }

    /// <summary>
    /// Gets all currently active sessions.
    /// </summary>
    /// <returns>An enumerable collection of active sessions.</returns>
    public IEnumerable<UdpSession> GetActiveSessions()
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var session in _sessions.Values)
        {
            if (now - session.LastActivity <= timeout)
                yield return session;
            else
                _sessions.TryRemove(session.EndPoint, out _);
        }
    }

    /// <summary>
    /// Removes a session for the given endpoint.
    /// </summary>
    /// <param name="endPoint">The endpoint to remove the session for.</param>
    public void Remove(IPEndPoint endPoint) => _sessions.TryRemove(endPoint, out _);
}
