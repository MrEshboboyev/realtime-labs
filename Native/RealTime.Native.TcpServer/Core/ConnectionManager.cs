using RealTime.Native.TcpServer.Abstractions;
using System.Collections.Concurrent;

namespace RealTime.Native.TcpServer.Core;

public class ConnectionManager
{
    // Barcha faol ulanishlar lug'ati
    private readonly ConcurrentDictionary<Guid, IConnection> _connections = new();
    // XonaId -> Mijozlar ro'yxati (HashSet tezkor qidiruv uchun)
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _rooms = new();

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

    // rooms
    public void JoinRoom(string roomId, Guid clientId)
    {
        var clients = _rooms.GetOrAdd(roomId, _ => []);
        lock (clients) 
        { 
            clients.Add(clientId);
        }
    }

    public void LeaveRoom(string roomId, Guid clientId)
    {
        if (_rooms.TryGetValue(roomId, out var clients))
        {
            lock (clients) 
            { 
                clients.Remove(clientId); 
            }
        }
    }

    public IEnumerable<IConnection> GetRoomClients(string roomId)
    {
        if (_rooms.TryGetValue(roomId, out var clientIds))
        {
            lock (clientIds)
            {
                return clientIds
                    .Select(id => _connections.TryGetValue(id, out var conn) ? conn : null)
                    .Where(c => c != null)!;
            }
        }
        return [];
    }
}
