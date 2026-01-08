using System.Net;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Core;
using RealTime.Native.Udp.Factories;
using RealTime.Native.Udp.Configuration;

// 1. Infrastructure
var logger = new SharedLogger("UDP-SERVER");
var serializer = new BinarySerializer();
var configuration = new UdpConfiguration();
var sessionManager = new SessionManager(configuration.ClientTimeout); // Remove inactive sessions based on config
var server = UdpFactory.CreateServer(configuration);

// 2. Simple room management (IPEndPoint collection)
var rooms = new Dictionary<string, HashSet<IPEndPoint>>();

logger.Log(LogLevel.Info, "UDP Server initializing...");

// 3. Message processing logic
server.MessageReceived += async (sender, package) =>
{
    // Add this log: Is packet actually arriving?
    logger.Log(LogLevel.Info, $"Raw packet received! Size: {package.Data.Length} bytes. From: {package.RemoteEndPoint}");

    try
    {
        var udpPacket = serializer.Deserialize<UdpPacket>(package.Data.ToArray());
        if (udpPacket == null)
        {
            logger.Log(LogLevel.Warning, "Failed to deserialize UdpPacket!");
            return;
        }

        if (udpPacket.RequiresAck)
        {
            // Tasdiq paketini tayyorlaymiz
            var ackCmd = new CommandPackage(CommandType.Ack, "SYSTEM", udpPacket.PacketId.ToString());
            var ackPayload = serializer.Serialize(ackCmd);
            var ackPacket = new UdpPacket(Guid.NewGuid(), 0, ackPayload, false); // ACK o'zi ACK talab qilmaydi

            byte[] ackData = serializer.Serialize(ackPacket);
            await server.SendAsync(ackData, package.RemoteEndPoint!);
        }

        var command = serializer.Deserialize<CommandPackage>(udpPacket.Payload);
        if (command == null)
        {
            logger.Log(LogLevel.Warning, "Failed to deserialize CommandPackage!");
            return;
        }

        var senderEp = package.RemoteEndPoint;
        if (senderEp == null) return;

        switch (command.Type)
        {
            case CommandType.JoinRoom:
                if (!rooms.ContainsKey(command.RoomId)) rooms[command.RoomId] = new();
                lock (rooms[command.RoomId]) { rooms[command.RoomId].Add(senderEp); }
                logger.Log(LogLevel.Success, $"[JOIN] {senderEp} joined room: {command.RoomId}");
                break;

            case CommandType.SendMessage:
                // Don't log PING messages (to avoid cluttering the terminal)
                if (command.Content != "PING")
                {
                    logger.Log(LogLevel.Info, $"[MESSAGE] {command.SenderName} ({senderEp}): {command.Content}");
                }

                if (rooms.TryGetValue(command.RoomId, out var clients))
                {
                    // IMPORTANT: Remove senderEp.ToString(), 
                    // because client sends their name in CommandPackage.
                    byte[] responsePayload = serializer.Serialize(command);

                    var responsePacket = new UdpPacket(Guid.NewGuid(), 0, responsePayload);
                    byte[] finalData = serializer.Serialize(responsePacket);

                    foreach (var clientEp in clients)
                    {
                        // Broadcast message to all room members
                        await server.SendAsync(finalData, clientEp);
                        logger.Log(LogLevel.Info, $"Broadcast sent: {clientEp}");
                    }
                }
                break;
        }
    }
    catch (Exception ex)
    {
        logger.Log(LogLevel.Error, $"Error: {ex.Message}");
    }
};

// Client activity (convenient place for Join Room logic)
server.ClientActivity += (sender, ep) =>
{
    // logger.Log(LogLevel.Info, $"Activity: {ep}");
};

// 4. Start the server
await server.StartAsync(5001); // UDP port: 5001
