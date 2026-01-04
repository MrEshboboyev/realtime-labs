namespace RealTime.Native.TcpClient.Events;

public class OnErrorEventArgs(
    Exception ex,
    string context
) : EventArgs
{
    public Exception Exception { get; } = ex;
    public string Context { get; } = context; // Xatolik qayerda yuz berdi? (Connect, Send, Receive)
    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
