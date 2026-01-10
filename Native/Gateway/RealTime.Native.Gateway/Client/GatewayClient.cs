using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpClient.Core;
using RealTime.Native.TcpClient.Events;
using RealTime.Native.Udp.Core;

namespace RealTime.Native.Gateway.Client;

public class GatewayClient
{
    private readonly NativeTcpClient _tcpClient;
    private readonly NativeUdpClient _udpClient;
    private readonly BinarySerializer _serializer = new();

    private Guid? _sessionId; // Serverdan keladigan pasport
    public bool IsConnected => _sessionId.HasValue;

    public GatewayClient(ClientOptions options)
    {
        _tcpClient = new NativeTcpClient(options);
        _udpClient = new NativeUdpClient();

        _tcpClient.OnMessageReceived += (s, e) => {
            if (e.RawData.Length > 0)
            {
                Console.WriteLine($"[DEBUG] TCP xabar keldi, hajmi: {e.RawData.Length} byte");
                HandleServerMessage(s, e);
            }
            else
            {
                Console.WriteLine("[DEBUG] Bo'sh TCP xabar keldi (0 byte) - Framing xatosi bo'lishi mumkin.");
            }
        };
    }

    public async Task ConnectAsync(string host, int udpPort)
    {
        // Xatolik tuzatildi: TCP ulanish (Options ichidagi Host:Port ishlatiladi)
        // NativeTcpClient.ConnectAsync parametrsiz ishlaydi (ct dan tashqari)
        await _tcpClient.ConnectAsync();

        // 2. UDP-ni tayyorlaymiz
        await _udpClient.ConnectAsync(host, udpPort);
    }

    private void HandleServerMessage(object? sender, OnMessageEventArgs e)
    {
        var command = _serializer.Deserialize<CommandPackage>(e.RawData.ToArray());

        if (command?.Type == CommandType.JoinRoom && command.RoomId == "SYSTEM")
        {
            if (Guid.TryParse(command.Content, out Guid sid))
            {
                _sessionId = sid;
                Console.WriteLine($"[GATEWAY] Handshake muvaffaqiyatli! ID: {_sessionId}");
                _ = SendUdpHello();
            }
        }
    }

    private async Task SendUdpHello()
    {
        if (!_sessionId.HasValue) return;

        // Server bizni tanib olishi uchun SessionId bilan UDP paket yuboramiz
        var hello = new CommandPackage(CommandType.JoinRoom, "LOBBY", "UDP_INIT", "Client", _sessionId);
        await SendAsync(hello, reliable: false);
    }

    public async Task SendAsync(CommandPackage command, bool reliable = false)
    {
        // Har bir xabarga avtomatik SessionId ni yopishtiramiz
        var commandWithSid = command with { SessionId = _sessionId };

        if (reliable)
        {
            // TCP orqali yuborish (NativeTcpClient.SendAsync generic ishlaydi)
            await _tcpClient.SendAsync(commandWithSid);
        }
        else
        {
            // UDP orqali yuborish
            await _udpClient.SendAsync(commandWithSid);
        }
    }
}
