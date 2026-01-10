using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpServer.Abstractions;
using RealTime.Native.TcpServer.Core;
using RealTime.Native.Udp.Core;
using System.Collections.Concurrent;

namespace RealTime.Native.Gateway;

public class GatewayServer
{
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, byte>> _rooms = new();
    private readonly TcpServerListener _tcpServer = new(new ServerOptions
    {
        MaxConnections = 1000,
        ReceiveBufferSize = 8192
    });

    private readonly UdpServerListener _udpServer = new(TimeSpan.FromSeconds(30));
    private readonly BinarySerializer _serializer = new();
    private readonly SharedLogger _logger = new("GATEWAY-SERVER");

    private readonly ConcurrentDictionary<Guid, IGatewaySession> _sessions = new();

    public async Task StartAsync(int tcpPort, int udpPort)
    {
        _tcpServer.ClientDisconnected += (s, connectionId) =>
        {
            var sessionKvp = _sessions.FirstOrDefault(x => x.Value is GatewaySession gs &&
                                                       ((IConnection)typeof(GatewaySession)
                                                       .GetField("_connection", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
                                                       .GetValue(gs)!).Id == connectionId);

            if (sessionKvp.Key != Guid.Empty)
            {
                _sessions.TryRemove(sessionKvp.Key, out _);
                _logger.Log(LogLevel.Warning, $"Sessiya yopildi: {sessionKvp.Key}");
            }
        };

        _udpServer.MessageReceived += HandleUdpMessage;

        _logger.Log(LogLevel.Info, $"Gateway ishga tushmoqda... TCP:{tcpPort}, UDP:{udpPort}");

        await Task.WhenAll(
            _tcpServer.StartAsync(tcpPort),
            _udpServer.StartAsync(udpPort)
        );
    }

    private async void HandleTcpConnection(object? sender, IConnection connection)
    {
        var sessionId = Guid.NewGuid();

        var session = new GatewaySession(
            sessionId, 
            connection,
            _udpServer.SendAsync,
            _serializer);

        _sessions.TryAdd(sessionId, session);

        var remoteIp = connection.Client.Client.RemoteEndPoint?.ToString() ?? "Unknown";

        _logger.Log(LogLevel.Success, $"Yangi TCP sessiya: {sessionId} (IP: {remoteIp})");

        var handshake = new CommandPackage(
            CommandType.JoinRoom,
            "SYSTEM",
            sessionId.ToString(),
            "SERVER",
            sessionId);

        await session.SendAsync(handshake, reliable: true);
    }

    private async void HandleUdpMessage(object? sender, TransportPackage package)
    {
        try
        {
            var udpPacket = _serializer.Deserialize<UdpPacket>(package.Data.ToArray());
            if (udpPacket == null) return;

            var command = _serializer.Deserialize<CommandPackage>(udpPacket.Payload);

            if (command?.SessionId != null && _sessions.TryGetValue(command.SessionId.Value, out var session))
            {
                if (session.UdpEndPoint == null && package.RemoteEndPoint != null)
                {
                    session.UdpEndPoint = package.RemoteEndPoint;
                    _logger.Log(LogLevel.Success, $"Sessiya {session.SessionId} UDP bilan bog'landi: {package.RemoteEndPoint}");
                }

                session.UpdateActivity();
                await ProcessCommand(session, command);
            }
        }
        catch (Exception ex)
        {
            _logger.Log(LogLevel.Error, $"UDP xabarini ishlashda xato: {ex.Message}");
        }
    }

    private async Task ProcessCommand(IGatewaySession session, CommandPackage command)
    {
        switch (command.Type)
        {
            case CommandType.JoinRoom:
                // 1. Foydalanuvchini xonaga qo'shish
                var room = _rooms.GetOrAdd(command.RoomId, _ => new ConcurrentDictionary<Guid, byte>());
                room.TryAdd(session.SessionId, 0);

                session.UserName = command.SenderName;
                _logger.Log(LogLevel.Info, $"{session.UserName} {command.RoomId} xonasiga kirdi.");

                // 2. Xonadagilarga yangi odam kelgani haqida xabar berish (TCP orqali)
                var joinNotify = command with { Content = $"{session.UserName} joined the room." };
                await BroadcastToRoomAsync(command.RoomId, joinNotify);
                break;

            case CommandType.SendMessage:
                // Chat xabarini xonadagilarga tarqatish (UDP orqali - tezkor)
                await BroadcastToRoomAsync(command.RoomId, command, exceptSessionId: session.SessionId);
                break;

            case CommandType.Ack:
                // RUDP ACK mantiqi (agar server-side reliable UDP ishlatsangiz)
                break;
        }
    }

    public async Task BroadcastToRoomAsync(
        string roomId,
        CommandPackage message,
        Guid? exceptSessionId = null)
    {
        if (!_rooms.TryGetValue(roomId, out var sessionIds)) return;

        var tasks = sessionIds.Keys
            .Where(sid => sid != exceptSessionId)
            .Select(sid =>
            {
                if (_sessions.TryGetValue(sid, out var session))
                {
                    // Xabar turi 'SendMessage' bo'lsa UDP (unreliable)
                    // 'Join/Leave' bo'lsa TCP (reliable) ishlatamiz
                    bool isReliable = message.Type != CommandType.SendMessage;
                    return session.SendAsync(message, isReliable);
                }
                return Task.CompletedTask;
            });

        await Task.WhenAll(tasks);
    }
}
