namespace RealTime.Native.Common.Models;

public enum CommandType 
{ 
    JoinRoom,
    LeaveRoom, 
    SendMessage, 
    SystemAlert 
}

public record CommandPackage(
    CommandType Type,
    string RoomId,
    string Content,
    string SenderName = ""
);
