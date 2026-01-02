using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpServer.Core;

// 1. Infratuzilmani sozlash
var logger = new SharedLogger("Server");
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

// Xabar kelganda (Eng asosiy qism)
server.MessageReceived += async (s, package) =>
{
    try
    {
        // 1. Kelgan baytlarni stringga o'girish (BinarySerializer yordamida)
        string message = binarySerializer.Deserialize<string>(package.Data.ToArray()) ?? "";
        logger.Log(LogLevel.Info, $"[MESSAGE] {package.ConnectionId.ToString()[..8]}: {message}");

        // 2. Broadcast mantiqi: Xabarni barcha mijozlarga tarqatish
        byte[] responseData = binarySerializer.Serialize($"Client {package.ConnectionId.ToString()[..4]} yozdi: {message}");

        // MUHIM: Xabarni tarmoqqa chiqarishdan oldin FrameWrap qilish shart!
        byte[] framedResponse = frameHandler.Wrap(responseData);

        var connections = server.GetConnectionManager().GetAllConnections();
        foreach (var conn in connections)
        {
            // Xabarni yuborgan mijozning o'ziga qaytarib o'tirmaymiz (ixtiyoriy)
            if (conn.Id != package.ConnectionId)
            {
                await conn.SendAsync(framedResponse);
            }
        }
    }
    catch (Exception ex)
    {
        logger.Log(LogLevel.Error, "Xabarni qayta ishlashda xatolik", ex);
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
