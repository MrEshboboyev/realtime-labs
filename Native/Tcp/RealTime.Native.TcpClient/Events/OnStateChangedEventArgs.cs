using RealTime.Native.Common.Models;

namespace RealTime.Native.TcpClient.Events;

public class OnStateChangedEventArgs(
    ConnectionState state,
    string message = ""
) : EventArgs
{
    public ConnectionState NewState { get; } = state;
    public string Message { get; } = message;
}
