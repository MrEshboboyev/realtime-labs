using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Core;

var logger = new SharedLogger("UDP-CLIENT");
Console.Write("Ismingizni kiriting: ");
string userName = Console.ReadLine() ?? "Guest";

try
{
    var client = new NativeUdpClient();
    var serializer = new BinarySerializer();

    client.MessageReceived += (s, data) =>
    {
        try
        {
            var command = serializer.Deserialize<CommandPackage>(data);
            if (command != null && command.Type == CommandType.SendMessage && command.Content != "PING")
            {
                // MUHIM: Hozirgi yozilayotgan qatorni tozalab, xabarni chiqarish
                // Bu ReadLine'ni kutib turgan kursorni "buzib" xabarni ko'rsatadi
                string message = $"[ROOM] {command.SenderName}: {command.Content}";

                // Konsolning pastki qatoriga o'tmay, yangi qator ochish
                Console.WriteLine("\r" + message);
                Console.Write("> "); // Kursorni qayta tiklash
            }
        }
        catch { }
    };

    await client.ConnectAsync("127.0.0.1", 5001);

    var joinCmd = new CommandPackage(CommandType.JoinRoom, "GAMING_ZONE", "", userName);
    await client.SendAsync(joinCmd);

    // Xabar yuborish sikli
    while (true)
    {
        // Kursor tayyor tursin
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input == "exit") break;
        if (string.IsNullOrWhiteSpace(input)) continue;

        await client.SendAsync(new CommandPackage(CommandType.SendMessage, "GAMING_ZONE", input, userName));
    }
}
catch (Exception ex)
{
    logger.Log(LogLevel.Error, "Xatolik!", ex);
}
