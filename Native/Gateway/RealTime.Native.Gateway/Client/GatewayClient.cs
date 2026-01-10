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

    // Konstruktorda endi ClientOptions talab qilinadi
    public GatewayClient(ClientOptions options)
    {
        // 1. TCP Clientni options bilan yaratamiz
        _tcpClient = new NativeTcpClient(options);

        // 2. UDP Client (Hozircha bo'sh, ConnectAsync da host:port beriladi)
        _udpClient = new NativeUdpClient();

        // Xatolik tuzatildi: OnMessageReceived event'iga ulanamiz
        _tcpClient.OnMessageReceived += HandleServerMessage;
    }

    public async Task ConnectAsync(string host, int udpPort)
    {
        // Xatolik tuzatildi: TCP ulanish (Options ichidagi Host:Port ishlatiladi)
        // NativeTcpClient.ConnectAsync parametrsiz ishlaydi (ct dan tashqari)
        await _tcpClient.ConnectAsync();

        // 2. UDP-ni tayyorlaymiz
        await _udpClient.ConnectAsync(host, udpPort);
    }

    // OnMessageEventArgs turi ishlatildi
    private void HandleServerMessage(object? sender, OnMessageEventArgs e)
    {
        // e.Data - bu byte[] (Framing-dan o'tgan toza ma'lumot)
        var command = _serializer.Deserialize<CommandPackage>(e.RawData.ToArray());

        // Agar bu SYSTEM dan kelgan va ichida SessionId bo'lgan xabar bo'lsa
        if (command?.Type == CommandType.JoinRoom && command.RoomId == "SYSTEM")
        {
            if (Guid.TryParse(command.Content, out Guid sid))
            {
                _sessionId = sid;
                Console.WriteLine($"[GATEWAY] Handshake muvaffaqiyatli! Sessiya ID: {_sessionId}");

                // Endi UDP bog'lash uchun Hello yuboramiz
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
