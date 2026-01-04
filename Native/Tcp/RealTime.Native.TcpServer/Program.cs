using Microsoft.Extensions.DependencyInjection;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Common.Services;
using RealTime.Native.TcpServer.Abstractions;
using RealTime.Native.TcpServer.Core;
using RealTime.Native.TcpServer.Services;

// 1. Dependency Injection container setup
var services = new ServiceCollection();

// 2. Register services
services.AddCommonServices("SERVER")
    .AddServerServices(new ServerOptions
    {
        Port = 5000,
        MaxConnections = 100,
        ClientTimeout = TimeSpan.FromMinutes(5)
    });

var serviceProvider = services.BuildServiceProvider();

// 3. Get required services
var logger = serviceProvider.GetRequiredService<SharedLogger>();
var server = serviceProvider.GetRequiredService<IServer>();
var binarySerializer = serviceProvider.GetRequiredService<ISerializer>();
var frameHandler = serviceProvider.GetRequiredService<IFrameHandler>();
var connectionManager = serviceProvider.GetRequiredService<ConnectionManager>();

logger.Log(LogLevel.Info, "Server components loaded...");

// 4. Subscribe to events

// When client connects
server.ClientConnected += (s, connection) =>
{
    logger.Log(LogLevel.Info, $"[CONNECTED] ID: {connection.Id} | IP: {connection.Client.Client.RemoteEndPoint}");
};

// When client disconnects
server.ClientDisconnected += (s, connectionId) =>
{
    logger.Log(LogLevel.Warning, $"[DISCONNECTED] ID: {connectionId}");
};

// When message is received (Room and Command logic)
server.MessageReceived += async (s, package) =>
{
    try
    {
        // 1. Deserialize package as CommandPackage
        var command = binarySerializer.Deserialize<CommandPackage>(package.Data.ToArray());
        if (command == null) return;

        switch (command.Type)
        {
            case CommandType.JoinRoom:
                connectionManager.JoinRoom(command.RoomId, package.ConnectionId);
                logger.Log(LogLevel.Info, $"[ROOM] {package.ConnectionId.ToString()[..4]} -> '{command.RoomId}' room joined.");

                // Confirmation message (only to the joining user)
                var welcome = binarySerializer.Serialize(new CommandPackage(CommandType.SystemAlert, command.RoomId, $"You have joined room {command.RoomId}!"));
                await connectionManager.GetConnection(package.ConnectionId)?.SendAsync(frameHandler.Wrap(welcome))!;
                break;

            case CommandType.SendMessage:
                logger.Log(LogLevel.Info, $"[MSG] Room: {command.RoomId} | From: {package.ConnectionId.ToString()[..4]} : {command.Content}");

                // Send message only to clients in the same room
                byte[] responseData = binarySerializer.Serialize(
                    command with 
                    { 
                        SenderName = package.ConnectionId.ToString()[..4] 
                    });
                byte[] framedResponse = frameHandler.Wrap(responseData);

                var roomClients = connectionManager.GetRoomClients(command.RoomId);
                foreach (var conn in roomClients)
                {
                    // Don't send back to the sender (optional)
                    if (conn.Id != package.ConnectionId)
                        await conn.SendAsync(framedResponse);
                }
                break;
        }
    }
    catch (Exception ex)
    {
        logger.Log(LogLevel.Error, "Error processing command", ex);
    }
};

// 5. Start the server
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (s, e) =>
{
    e.Cancel = true; // Catch Control+C
    logger.Log(LogLevel.Critical, "Server shutdown signal received...");
    cts.Cancel(); // Send "stop" signal to all processes
};

try
{
    logger.Log(LogLevel.Info, $"TCP Server starting on port 5000...");

    // Start the server and wait for it to finish or be cancelled
    await server.StartAsync(5000, cts.Token);
}
catch (OperationCanceledException)
{
    // This is expected, not an error
    logger.Log(LogLevel.Info, "Server stopped via cancellation.");
}
catch (Exception ex)
{
    logger.Log(LogLevel.Critical, "Server unexpectedly stopped", ex);
}
finally
{
    if (server is IAsyncDisposable asyncDisposable)
        await asyncDisposable.DisposeAsync();
    else if (server is IDisposable disposable)
        disposable.Dispose();
    
    logger.Log(LogLevel.Info, "Application ended.");
}
