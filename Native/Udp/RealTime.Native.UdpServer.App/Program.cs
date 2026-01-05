using System.Net;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Core;

// 1. Infratuzilma
var logger = new SharedLogger("UDP-SERVER");
var serializer = new BinarySerializer();
var sessionManager = new SessionManager(TimeSpan.FromSeconds(30)); // 30 sekunda faol bo'lmasa o'chirish
var server = new UdpServerListener(TimeSpan.FromSeconds(30));

// 2. Oddiy xona boshqaruvi (IPEndPointlar ro'yxati)
var rooms = new Dictionary<string, HashSet<IPEndPoint>>();

logger.Log(LogLevel.Info, "UDP Server tayyorlanmoqda...");

// 3. Xabar kelganda bajariladigan mantiq
server.MessageReceived += async (s, package) =>
{
    // 1. MANA BU LOGNI QO'SHING: Paket umuman kelyaptimi?
    logger.Log(LogLevel.Info, $"Raw paket keldi! Hajmi: {package.Data.Length} bayt. Kimdan: {package.RemoteEndPoint}");

    try
    {
        var udpPacket = serializer.Deserialize<UdpPacket>(package.Data.ToArray());
        if (udpPacket == null)
        {
            logger.Log(LogLevel.Warning, "UdpPacket deserialize bo'lmadi!");
            return;
        }

        var command = serializer.Deserialize<CommandPackage>(udpPacket.Payload);
        if (command == null)
        {
            logger.Log(LogLevel.Warning, "CommandPackage deserialize bo'lmadi!");
            return;
        }

        var senderEp = package.RemoteEndPoint;
        if (senderEp == null) return;

        switch (command.Type)
        {
            case CommandType.JoinRoom:
                if (!rooms.ContainsKey(command.RoomId)) rooms[command.RoomId] = new();
                lock (rooms[command.RoomId]) { rooms[command.RoomId].Add(senderEp); }
                logger.Log(LogLevel.Success, $"[JOIN] {senderEp} xonaga kirdi: {command.RoomId}");
                break;

            case CommandType.SendMessage:
                logger.Log(LogLevel.Info, $"[MESSAGE] {senderEp}: {command.Content}");
                if (rooms.TryGetValue(command.RoomId, out var clients))
                {
                    // Xabarni tayyorlash
                    byte[] responsePayload = serializer.Serialize(command with { SenderName = senderEp.ToString() });
                    var responsePacket = new UdpPacket(Guid.NewGuid(), 0, responsePayload);
                    byte[] finalData = serializer.Serialize(responsePacket);

                    foreach (var clientEp in clients)
                    {
                        await server.SendAsync(finalData, clientEp);
                    }
                }
                break;
        }
    }
    catch (Exception ex)
    {
        logger.Log(LogLevel.Error, $"Xatolik: {ex.Message}");
    }
};

// Yangi mijoz faolligi (Join Room mantiqi uchun qulay joy)
server.ClientActivity += (s, ep) =>
{
    // logger.Log(LogLevel.Info, $"Faollik: {ep}");
};

// 4. Serverni yurgizish
await server.StartAsync(5001); // UDP port: 5001
