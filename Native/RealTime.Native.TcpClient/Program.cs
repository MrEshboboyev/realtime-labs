using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.TcpClient.Core;

// 1. Sozlamalarni o'rnatamiz
var options = new ClientOptions
{
    Host = "127.0.0.1",
    Port = 5000,
    AutoReconnect = true,          // Avtomatik qayta ulanish yoniq
    MaxRetryAttempts = 10,         // 10 martagacha urinish
    ReconnectDelay = TimeSpan.FromSeconds(2) // Har safar kutish vaqti ortib boradi
};

// 2. Logger va Clientni yaratamiz
var logger = new SharedLogger("CLIENT");
var client = new NativeClient(options);
var serializer = new BinarySerializer();
// Test uchun bitta xona nomi
string currentRoom = "DEFAULT_CHAT";

logger.Log(LogLevel.Info, "Mijoz ishga tushmoqda...");

// 3. Voqealarga (Events) obuna bo'lamiz
client.OnConnected += (s, e) =>
{
    logger.Log(LogLevel.Success, "Serverga ulanish muvaffaqiyatli amalga oshirildi!");
};

client.OnDisconnected += (s, e) =>
{
    logger.Log(LogLevel.Warning, "Server bilan aloqa uzildi.");
};

// 3. Xabarni qabul qilish qismini yangilaymiz
client.OnMessageReceived += (s, e) =>
{
    var cmd = serializer.Deserialize<CommandPackage>(e.RawData.ToArray());
    if (cmd != null)
    {
        if (cmd.Type == CommandType.SystemAlert)
            logger.Log(LogLevel.Success, $"[SYSTEM]: {cmd.Content}");
        else
            logger.Log(LogLevel.Info, $"[{cmd.RoomId}] {cmd.SenderName}: {cmd.Content}");
    }
};

client.OnError += (s, e) =>
{
    logger.Log(LogLevel.Error, $"Xatolik yuz berdi ({e.Context}): {e.Exception.Message}");
};

// 4. Serverga ulanish
try
{
    await client.ConnectAsync();
}
catch (Exception ex)
{
    logger.Log(LogLevel.Critical, "Dastlabki ulanishda xato, lekin ReconnectionManager fonda ishlashni boshlaydi.");
}

await client.SendAsync(new CommandPackage(CommandType.JoinRoom, currentRoom, ""));

// 5. Konsol orqali muloqot qilish
logger.Log(LogLevel.Info, "Xabar yuborish uchun matn yozing va Enter bosing (Chiqish uchun 'exit'):");

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    // Xonani o'zgartirish buyrug'i (masalan: /join it_park)
    if (input.StartsWith("/join "))
    {
        currentRoom = input.Replace("/join ", "").Trim();
        await client.SendAsync(new CommandPackage(CommandType.JoinRoom, currentRoom, ""));
        continue;
    }

    if (client.IsConnected)
    {
        var msgPayload = new CommandPackage(CommandType.SendMessage, currentRoom, input);
        await client.SendAsync(msgPayload);
    }
}

await client.DisconnectAsync();
logger.Log(LogLevel.Info, "Dastur yakunlandi.");
