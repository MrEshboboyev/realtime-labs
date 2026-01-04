namespace RealTime.Native.Common.Models;

/// <summary>
/// Defines the types of commands supported by the system
/// </summary>
public enum CommandType 
{ 
    /// <summary>
    /// Command to join a chat room
    /// </summary>
    JoinRoom,
    
    /// <summary>
    /// Command to leave a chat room
    /// </summary>
    LeaveRoom, 
    
    /// <summary>
    /// Command to send a message
    /// </summary>
    SendMessage, 
    
    /// <summary>
    /// System-generated alert message
    /// </summary>
    SystemAlert 
}
