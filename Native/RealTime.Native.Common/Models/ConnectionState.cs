namespace RealTime.Native.Common.Models;

/// <summary>
/// Represents the possible states of a network connection
/// </summary>
public enum ConnectionState
{
    /// <summary>
    /// Connection is in progress
    /// </summary>
    Connecting,
    
    /// <summary>
    /// Connection is established
    /// </summary>
    Connected,
    
    /// <summary>
    /// Connection is being terminated
    /// </summary>
    Disconnecting,
    
    /// <summary>
    /// Connection is terminated
    /// </summary>
    Disconnected,
    
    /// <summary>
    /// Connection is attempting to reconnect
    /// </summary>
    Reconnecting
}
