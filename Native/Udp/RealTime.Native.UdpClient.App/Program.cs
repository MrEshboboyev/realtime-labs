using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Core;

var logger = new SharedLogger("UDP-CLIENT");
var client = new NativeUdpClient();
var serializer = new BinarySerializer();

client.MessageReceived += (s, data) =>
{
    var command = serializer.Deserialize<CommandPackage>(data);
    if (command != null)
    {
        logger.Log(LogLevel.Info, $"[UDP-ROOM]: {command.SenderName}: {command.Content}");
    }
};

await client.ConnectAsync("127.0.0.1", 5001);

// Xonaga kirish buyrug'i
var joinCmd = new CommandPackage(CommandType.JoinRoom, "GAMING_ZONE", "");
await client.SendAsync(joinCmd);

logger.Log(LogLevel.Success, "UDP Chatga xush kelibsiz! Xabar yozing:");

while (true)
{
    var input = Console.ReadLine();
    if (input == "exit") break;

    var msg = new CommandPackage(CommandType.SendMessage, "GAMING_ZONE", input ?? "");
    await client.SendAsync(msg);
}

await client.DisconnectAsync();
