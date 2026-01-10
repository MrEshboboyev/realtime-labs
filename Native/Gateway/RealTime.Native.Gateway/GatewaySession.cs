using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpServer.Abstractions;
using System.Net;

namespace RealTime.Native.Gateway;

public class GatewaySession : IGatewaySession
{
    public Guid SessionId { get; }
    public string UserName { get; set; } = "Guest";
    public IPEndPoint? UdpEndPoint { get; set; }
    public DateTimeOffset LastActivity { get; private set; }

    private readonly IConnection _connection;
    private readonly Func<byte[], IPEndPoint, Task> _udpSender;
    private readonly BinarySerializer _serializer;

    public GatewaySession(
        Guid sessionId,
        IConnection connection, 
        Func<byte[], IPEndPoint, Task> udpSender,
        BinarySerializer serializer)
    {
        SessionId = sessionId;
        _connection = connection;
        _udpSender = udpSender;
        _serializer = serializer;
        UpdateActivity();
    }

    public void UpdateActivity() => LastActivity = DateTimeOffset.UtcNow;

    public async Task SendAsync<T>(T message, bool reliable = false)
    {
        var data = _serializer.Serialize(message);
        Console.WriteLine($"[DEBUG] Server yubormoqda: {data.Length} byte");

        if (reliable || UdpEndPoint == null)
        {
            await _connection.SendAsync(data);
        }
        else
        {
            await _udpSender(data, UdpEndPoint);
        }
    }
}
