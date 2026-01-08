using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Configuration;
using RealTime.Native.Udp.Factories;

var logger = new SharedLogger("UDP-CLIENT");
Console.Write("Enter your name: ");
string userName = Console.ReadLine() ?? "Guest";

try
{
    var configuration = new UdpConfiguration();
    var client = UdpFactory.CreateClient(configuration);
    var serializer = new BinarySerializer();

    client.MessageReceived += (sender, data) =>
    {
        try
        {
            var command = serializer.Deserialize<CommandPackage>(data);
            if (command != null && command.Type == CommandType.SendMessage && command.Content != "PING")
            {
                // IMPORTANT: Clear the current input line and display the message
                // This "breaks" the ReadLine cursor to show the message
                string message = $"[ROOM] {command.SenderName}: {command.Content}";

                // Print message without moving to next line
                Console.WriteLine("\r" + message);
                Console.Write("> "); // Restore cursor position
            }
        }
        catch { }
    };

    await client.ConnectAsync("127.0.0.1", 5001);

    var joinCommand = new CommandPackage(CommandType.JoinRoom, "GAMING_ZONE", "", userName);
    await client.SendReliableAsync(joinCommand);

    // Message sending loop
    while (true)
    {
        // Keep cursor ready
        Console.Write("> ");
        var input = Console.ReadLine();

        if (input == "exit") break;
        if (string.IsNullOrWhiteSpace(input)) continue;

        await client.SendReliableAsync(new CommandPackage(
            CommandType.SendMessage, 
            "GAMING_ZONE",
            input, 
            userName));
    }
}
catch (Exception ex)
{
    logger.Log(LogLevel.Error, "Error!", ex);
}
