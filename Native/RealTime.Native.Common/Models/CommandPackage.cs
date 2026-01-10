namespace RealTime.Native.Common.Models;

/// <summary>
/// Represents a command package containing command type, room, content and sender information
/// </summary>
public record CommandPackage(
    /// <summary>
    /// The type of command
    /// </summary>
    CommandType Type,
    
    /// <summary>
    /// The room ID for the command
    /// </summary>
    string RoomId,
    
    /// <summary>
    /// The content of the command
    /// </summary>
    string Content,
    
    /// <summary>
    /// The name of the sender (optional)
    /// </summary>
    string SenderName = "",

    // Gateway sessiyasini tanish uchun
    Guid? SessionId = null
);
