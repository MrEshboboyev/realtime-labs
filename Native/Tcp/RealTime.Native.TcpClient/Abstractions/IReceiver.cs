using System.Net.Sockets;

namespace RealTime.Native.TcpClient.Abstractions;

internal interface IReceiver
{
    // Soketdan baytlarni o'qish jarayonini boshlash
    Task StartReceivingAsync(NetworkStream stream, CancellationToken ct);

    // O'qishni to'xtatish
    void Stop();
}
