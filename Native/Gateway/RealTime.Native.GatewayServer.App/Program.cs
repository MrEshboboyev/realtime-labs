using RealTime.Native.Gateway;

var gateway = new GatewayServer();
// TCP port: 5000, UDP port: 5001
await gateway.StartAsync(5000, 5001);

Console.WriteLine("Gateway Server ishlamoqda. To'xtatish uchun Enter bosing...");
Console.ReadLine();
