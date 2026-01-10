using RealTime.Native.Common.Models;
using RealTime.Native.Gateway.Client;
using RealTime.Native.TcpClient.Core;

var options = new ClientOptions
{
    Host = "127.0.0.1",
    Port = 5000 // Gateway TCP porti
};

var client = new GatewayClient(options);

// Ulanish: Host va UDP port (5001)
await client.ConnectAsync("127.0.0.1", 5001);

Console.WriteLine("Serverga ulanish amalga oshirildi.");

// Test xabari yuboramiz
var message = new CommandPackage(
    CommandType.SendMessage,
    "LOBBY",
    "Salom, bu UDP orqali kelgan tezkor xabar!",
    "Ali"
);

while (true)
{
    var text = Console.ReadLine();
    if (text == "exit") break;

    var msg = message with { Content = text! };
    await client.SendAsync(msg, reliable: false); // false = UDP, true = TCP
}
