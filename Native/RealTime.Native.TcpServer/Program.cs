using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpServer.Core;

// 1. Infratuzilmani sozlash
var logger = new SharedLogger("SERVER");
var options = new ServerOptions
{
    Port = 5000,
    MaxConnections = 100,
    ClientTimeout = TimeSpan.FromMinutes(5)
};

// 2. Server va uning qurollarini yaratish
var server = new TcpServerListener(options);
var binarySerializer = new BinarySerializer();
var frameHandler = new LengthPrefixedFrame();

logger.Log(LogLevel.Info, "Server komponentlari yuklanmoqda...");

// 3. Voqealarga (Events) obuna bo'lish

// Mijoz ulanganda
server.ClientConnected += (s, connection) =>
{
    logger.Log(LogLevel.Info, $"[CONNECTED] ID: {connection.Id} | IP: {connection.Client.Client.RemoteEndPoint}");
};

// Mijoz uzilganda
server.ClientDisconnected += (s, connectionId) =>
{
    logger.Log(LogLevel.Warning, $"[DISCONNECTED] ID: {connectionId}");
};

// Xabar kelganda (Xonalar va Buyruqlar mantiqi)
server.MessageReceived += async (s, package) =>
{
    try
    {
        // 1. Paketni CommandPackage sifatida deserializatsiya qilish
        var command = binarySerializer.Deserialize<CommandPackage>(package.Data.ToArray());
        if (command == null) return;

        var manager = server.GetConnectionManager();

        switch (command.Type)
        {
            case CommandType.JoinRoom:
                manager.JoinRoom(command.RoomId, package.ConnectionId);
                logger.Log(LogLevel.Info, $"[ROOM] {package.ConnectionId.ToString()[..4]} -> '{command.RoomId}' xonasiga kirdi.");

                // Tasdiqlash xabari (faqat kiritgan odamga)
                var welcome = binarySerializer.Serialize(new CommandPackage(CommandType.SystemAlert, command.RoomId, $"Siz {command.RoomId} xonasiga kirdingiz!"));
                await manager.GetConnection(package.ConnectionId)?.SendAsync(frameHandler.Wrap(welcome))!;
                break;

            case CommandType.SendMessage:
                logger.Log(LogLevel.Info, $"[MSG] Room: {command.RoomId} | From: {package.ConnectionId.ToString()[..4]} : {command.Content}");

                // Xabarni faqat shu xonadagi mijozlarga yuborish
                byte[] responseData = binarySerializer.Serialize(
                    command with 
                    { 
                        SenderName = package.ConnectionId.ToString()[..4] 
                    });
                byte[] framedResponse = frameHandler.Wrap(responseData);

                var roomClients = manager.GetRoomClients(command.RoomId);
                foreach (var conn in roomClients)
                {
                    // Xabarni yuborgan odamning o'ziga qaytarmaslik (ixtiyoriy)
                    if (conn.Id != package.ConnectionId)
                        await conn.SendAsync(framedResponse);
                }
                break;
        }
    }
    catch (Exception ex)
    {
        logger.Log(LogLevel.Error, "Buyruqni bajarishda xatolik", ex);
    }
};

// 4. Serverni ishga tushirish
var cts = new CancellationTokenSource();

// Konsolni yopganda serverni chiroyli to'xtatish uchun
Console.CancelKeyPress += async (s, e) =>
{
    e.Cancel = true;
    logger.Log(LogLevel.Critical, "Server to'xtatilmoqda...");
    await server.StopAsync();
    cts.Cancel();
};

try
{
    logger.Log(LogLevel.Info, $"TCP Server {options.Port}-portda tinglashni boshlamoqda...");
    await server.StartAsync(options.Port, cts.Token);
}
catch (Exception ex)
{
    logger.Log(LogLevel.Critical, "Server kutilmaganda to'xtadi", ex);
}

// Dastur tugab qolmasligi uchun
await Task.Delay(-1);
