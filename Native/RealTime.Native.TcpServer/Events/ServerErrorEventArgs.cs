namespace RealTime.Native.TcpServer.Events;

public class ServerErrorEventArgs(Exception exception, string context) : EventArgs
{
    public Exception Exception { get; } = exception;
    public string Context { get; } = context;
    public DateTimeOffset OccurredAt { get; } = DateTimeOffset.UtcNow;
}
