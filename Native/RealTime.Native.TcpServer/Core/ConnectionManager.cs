using RealTime.Native.TcpServer.Abstractions;
using System.Collections.Concurrent;

namespace RealTime.Native.TcpServer.Core;

public class ConnectionManager
{
    // Barcha faol ulanishlar lug'ati
    private readonly ConcurrentDictionary<Guid, IConnection> _connections = new();

    public void AddConnection(IConnection connection)
    {
        _connections.TryAdd(connection.Id, connection);
    }

    public void RemoveConnection(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            connection.Dispose();
        }
    }

    public IConnection? GetConnection(Guid connectionId) =>
        _connections.GetValueOrDefault(connectionId);

    public IEnumerable<IConnection> GetAllConnections() => _connections.Values;

    public int Count => _connections.Count;

    public void Clear()
    {
        foreach (var conn in _connections.Values)
        {
            conn.Dispose();
        }
        _connections.Clear();
    }
}
