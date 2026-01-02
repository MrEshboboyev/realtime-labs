using RealTime.Native.Common.Infrastructure;
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

client.OnMessageReceived += (s, e) =>
{
    // Kelgan baytlarni stringga o'girish
    var message = serializer.Deserialize<string>(e.RawData.ToArray());
    logger.Log(LogLevel.Info, $"[SERVERDAN XABAR]: {message}");
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

// 5. Konsol orqali muloqot qilish
logger.Log(LogLevel.Info, "Xabar yuborish uchun matn yozing va Enter bosing (Chiqish uchun 'exit'):");

while (true)
{
    var input = Console.ReadLine();

    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.CurrentCultureIgnoreCase)) break;

    if (client.IsConnected)
    {
        try
        {
            await client.SendAsync(input);
        }
        catch (Exception ex)
        {
            logger.Log(LogLevel.Error, "Xabar yuborishda xato.");
        }
    }
    else
    {
        logger.Log(LogLevel.Warning, "Hozircha serverga ulanmagansiz. Qayta ulanish kutilmoqda...");
    }
}

await client.DisconnectAsync();
logger.Log(LogLevel.Info, "Dastur yakunlandi.");
