using Microsoft.Extensions.DependencyInjection;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Common.Services;
using RealTime.Native.TcpClient.Abstractions;
using RealTime.Native.TcpClient.Core;
using RealTime.Native.TcpClient.Services;

// 1. Dependency Injection container setup
var services = new ServiceCollection();

// 2. Register services
services.AddCommonServices("CLIENT")
    .AddClientServices(new ClientOptions
    {
        Host = "127.0.0.1",
        Port = 5000,
        AutoReconnect = true,          // Auto-reconnect enabled
        MaxRetryAttempts = 10,         // Up to 10 attempts
        ReconnectDelay = TimeSpan.FromSeconds(2) // Delay increases each time
    });

var serviceProvider = services.BuildServiceProvider();

// 3. Get required services
var logger = serviceProvider.GetRequiredService<SharedLogger>();
var client = serviceProvider.GetRequiredService<ITcpClient>();
var serializer = serviceProvider.GetRequiredService<ISerializer>();

// Test room name
string currentRoom = "DEFAULT_CHAT";

logger.Log(LogLevel.Info, "Client starting...");

// 4. Subscribe to events
client.OnConnected += (s, e) =>
{
    logger.Log(LogLevel.Success, "Successfully connected to server!");
};

client.OnDisconnected += (s, e) =>
{
    logger.Log(LogLevel.Warning, "Disconnected from server.");
};

// 4. Update message receiving logic
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
    logger.Log(LogLevel.Error, $"Error occurred ({e.Context}): {e.Exception.Message}");
};

// 5. Connect to server
try
{
    await client.ConnectAsync();
}
catch (Exception)
{
    logger.Log(LogLevel.Critical, "Initial connection failed, but ReconnectionManager will start in background.");
}

await client.SendAsync(new CommandPackage(CommandType.JoinRoom, currentRoom, ""));

// 6. Console-based communication
logger.Log(LogLevel.Info, "Enter text and press Enter to send messages (Type 'exit' to quit):\n");

while (true)
{
    var input = Console.ReadLine();
    if (string.IsNullOrWhiteSpace(input)) continue;
    if (input.Equals("exit", StringComparison.OrdinalIgnoreCase)) break;

    // Room change command (e.g., /join it_park)
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
logger.Log(LogLevel.Info, "Application ended.");

// Cleanup
if (client is IAsyncDisposable asyncDisposable)
    await asyncDisposable.DisposeAsync();
else if (client is IDisposable disposable)
    disposable.Dispose();

logger.Log(LogLevel.Info, "Client resources disposed.");
