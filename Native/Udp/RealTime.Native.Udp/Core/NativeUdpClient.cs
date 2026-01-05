using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Abstractions;
using RealTime.Native.Udp.Reliable;
using System.Net;

namespace RealTime.Native.Udp.Core;

public class NativeUdpClient : UdpBase, IUdpClient
{
    private IPEndPoint? _serverEndPoint;
    private readonly BinarySerializer _serializer = new();
    private readonly PacketSequencer _sequencer = new();
    private readonly CancellationTokenSource _cts = new();

    public event EventHandler<byte[]>? MessageReceived;
    public event EventHandler<Exception>? OnError;
    public bool IsActive { get; private set; }

    public NativeUdpClient() : base("UDP-CLIENT") { }

    public async Task ConnectAsync(string host, int port)
    {
        try
        {
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            IsActive = true;

            _ = Task.Run(() => ReceiveLoop(_cts.Token), _cts.Token);
            _ = Task.Run(() => HeartbeatLoop(_cts.Token), _cts.Token);

            Logger.Log(LogLevel.Success, $"UDP Client ulanishga tayyor: {host}:{port}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "Ulanishda xatolik", ex);
            throw;
        }
    }

    public async Task SendAsync<T>(T message)
    {
        if (!IsActive || _serverEndPoint == null) return;

        try
        {
            var payload = _serializer.Serialize(message);

            // Paketni UdpPacket ichiga o'raymiz (Sequence number bilan)
            var packet = new UdpPacket(
                Guid.NewGuid(),
                _sequencer.GetNextSequenceNumber(),
                payload
            );

            var finalData = _serializer.Serialize(packet);
            await SendRawAsync(finalData, _serverEndPoint);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    private async Task ReceiveLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await Socket.ReceiveAsync(ct);

                // Kelgan paketni UdpPacket sifatida ochamiz
                var packet = _serializer.Deserialize<UdpPacket>(result.Buffer);
                if (packet == null) continue;

                // Sequencer orqali tartibga solib, MessageReceived'ga uzatamiz
                var orderedMessages = _sequencer.ProcessInOrder(packet.SequenceNumber, packet.Payload);
                foreach (var msg in orderedMessages)
                {
                    MessageReceived?.Invoke(this, msg);
                }
            }
            catch (Exception ex) when (!ct.IsCancellationRequested)
            {
                OnError?.Invoke(this, ex);
            }
        }
    }

    private async Task HeartbeatLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            // PING so'zini ham UdpPacket ichida yuboramiz
            var pingCmd = new CommandPackage(CommandType.SendMessage, "SYSTEM", "PING");
            await SendAsync(pingCmd);

            await Task.Delay(5000, ct);
        }
    }

    public Task DisconnectAsync()
    {
        IsActive = false;
        _cts.Cancel();
        Socket.Close();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        DisconnectAsync();

        GC.SuppressFinalize(this);
    }
}
