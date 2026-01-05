using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Core;

var logger = new SharedLogger("UDP-CLIENT");
logger.Log(LogLevel.Info, "Dastur boshlandi...");

try
{
    var client = new NativeUdpClient();
    var serializer = new BinarySerializer();

    client.OnError += (s, ex) => logger.Log(LogLevel.Error, $"XATO: {ex.Message}");

    logger.Log(LogLevel.Info, "Serverga ulanish...");
    await client.ConnectAsync("127.0.0.1", 5001);

    var joinCmd = new CommandPackage(CommandType.JoinRoom, "GAMING_ZONE", "");
    await client.SendAsync(joinCmd);

    logger.Log(LogLevel.Success, "Chat faol. Xabar yozing:");

    while (true)
    {
        var input = Console.ReadLine();
        if (string.IsNullOrEmpty(input) || input == "exit") break;
        await client.SendAsync(new CommandPackage(CommandType.SendMessage, "GAMING_ZONE", input));
    }
}
catch (Exception ex)
{
    logger.Log(LogLevel.Error, "Kutilmagan halokat!", ex);
}
