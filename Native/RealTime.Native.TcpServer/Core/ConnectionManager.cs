using RealTime.Native.TcpServer.Abstractions;
using System.Collections.Concurrent;
using RealTime.Native.Common.Infrastructure;

namespace RealTime.Native.TcpServer.Core;

/// <summary>
/// Manages client connections and room associations
/// </summary>
public class ConnectionManager(
    SharedLogger? logger = null
)
{
    private readonly SharedLogger? _logger = logger;
    
    // Dictionary of all active connections
    private readonly ConcurrentDictionary<Guid, IConnection> _connections = new();
    // RoomId -> Client IDs (HashSet for fast lookups)
    private readonly ConcurrentDictionary<string, HashSet<Guid>> _rooms = new();

    /// <summary>
    /// Adds a connection to the manager
    /// </summary>
    /// <param name="connection">The connection to add</param>
    public void AddConnection(IConnection connection)
    {
        if (_connections.TryAdd(connection.Id, connection))
        {
            _logger?.Log(LogLevel.Info, $"Connection {connection.Id} added to manager");
        }
    }

    /// <summary>
    /// Removes a connection from the manager
    /// </summary>
    /// <param name="connectionId">The ID of the connection to remove</param>
    public void RemoveConnection(Guid connectionId)
    {
        if (_connections.TryRemove(connectionId, out var connection))
        {
            _logger?.Log(LogLevel.Info, $"Connection {connectionId} removed from manager");
            connection.Dispose();
        }
    }

    /// <summary>
    /// Gets a connection by ID
    /// </summary>
    /// <param name="connectionId">The ID of the connection to retrieve</param>
    /// <returns>The connection if found, null otherwise</returns>
    public IConnection? GetConnection(Guid connectionId) =>
        _connections.GetValueOrDefault(connectionId);

    /// <summary>
    /// Gets all active connections
    /// </summary>
    /// <returns>Enumeration of all active connections</returns>
    public IEnumerable<IConnection> GetAllConnections() => _connections.Values;

    /// <summary>
    /// Gets the count of active connections
    /// </summary>
    public int Count => _connections.Count;

    /// <summary>
    /// Clears all connections and rooms
    /// </summary>
    public void Clear()
    {
        _logger?.Log(LogLevel.Info, "Clearing all connections");
        
        foreach (var conn in _connections.Values)
        {
            conn.Dispose();
        }
        _connections.Clear();
        _rooms.Clear();
    }

    /// <summary>
    /// Adds a client to a room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <param name="clientId">The client ID</param>
    public void JoinRoom(string roomId, Guid clientId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
        {
            _logger?.Log(LogLevel.Warning, "Attempted to join a null or empty room");
            return;
        }
        
        var clients = _rooms.GetOrAdd(roomId, _ => new HashSet<Guid>());
        lock (clients) 
        { 
            clients.Add(clientId);
        }
        
        _logger?.Log(LogLevel.Info, $"Client {clientId} joined room {roomId}");
    }

    /// <summary>
    /// Removes a client from a room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <param name="clientId">The client ID</param>
    public void LeaveRoom(string roomId, Guid clientId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !_rooms.TryGetValue(roomId, out var clients))
        {
            return;
        }
        
        lock (clients) 
        { 
            clients.Remove(clientId); 
        }
        
        _logger?.Log(LogLevel.Info, $"Client {clientId} left room {roomId}");
    }

    /// <summary>
    /// Gets all clients in a room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <returns>Enumeration of connections in the room</returns>
    public IEnumerable<IConnection> GetRoomClients(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !_rooms.TryGetValue(roomId, out var clientIds))
        {
            return [];
        }
        
        lock (clientIds)
        {
            return clientIds
                .Select(id => _connections.TryGetValue(id, out var conn) ? conn : null)
                .Where(c => c != null)!
                .ToList()!; // Convert to list to avoid issues with deferred execution
        }
    }
    
    /// <summary>
    /// Gets all room IDs
    /// </summary>
    /// <returns>Enumeration of all room IDs</returns>
    public IEnumerable<string> GetAllRoomIds() => _rooms.Keys;
    
    /// <summary>
    /// Gets the number of clients in a room
    /// </summary>
    /// <param name="roomId">The room ID</param>
    /// <returns>The number of clients in the room</returns>
    public int GetRoomClientCount(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId) || !_rooms.TryGetValue(roomId, out var clientIds))
        {
            return 0;
        }
        
        lock (clientIds)
        {
            return clientIds.Count;
        }
    }
}
