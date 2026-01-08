using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;
using RealTime.Native.Common.Protocols.Serialization;
using RealTime.Native.Udp.Abstractions;
using RealTime.Native.Udp.Reliable;
using System.Collections.Concurrent;
using System.Net;

namespace RealTime.Native.Udp.Core;

/// <summary>
/// Implementation of a UDP client that handles reliable communication with sequence numbers and heartbeats.
/// </summary>
public class NativeUdpClient : UdpBase, IUdpClient
{
    private IPEndPoint? _serverEndPoint;
    private readonly BinarySerializer _serializer = new();
    private readonly PacketSequencer _sequencer = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private readonly ConcurrentDictionary
        <Guid, 
        (UdpPacket Packet, DateTimeOffset SentTime, int RetryCount)> _pendingAcks = new();

    /// <summary>
    /// Occurs when a message is received from the remote endpoint.
    /// </summary>
    public event EventHandler<byte[]>? MessageReceived;

    /// <summary>
    /// Occurs when an error occurs during UDP communication.
    /// </summary>
    public event EventHandler<Exception>? OnError;

    /// <summary>
    /// Gets whether the client is currently active and connected.
    /// </summary>
    public bool IsActive { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="NativeUdpClient"/> class.
    /// </summary>
    public NativeUdpClient() : base("UDP-CLIENT") { }

    /// <summary>
    /// Connects the UDP client to the specified host and port.
    /// </summary>
    /// <param name="host">The host address to connect to.</param>
    /// <param name="port">The port number to connect to.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task ConnectAsync(string host, int port)
    {
        try
        {
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(host), port);
            IsActive = true;

            _ = Task.Run(() => ReceiveLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            _ = Task.Run(() => HeartbeatLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);
            _ = Task.Run(() => RetryLoop(_cancellationTokenSource.Token), _cancellationTokenSource.Token);

            Logger.Log(LogLevel.Success, $"UDP Client ready to connect: {host}:{port}");
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Error, "Connection error", ex);
            throw;
        }
    }

    /// <summary>
    /// Sends a message to the connected endpoint.
    /// </summary>
    /// <typeparam name="T">The type of the message to send.</typeparam>
    /// <param name="message">The message to send.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendAsync<T>(T message)
    {
        if (!IsActive || _serverEndPoint == null) return;

        try
        {
            var payload = _serializer.Serialize(message);

            // Wrap the packet in UdpPacket with sequence number
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

    public async Task SendReliableAsync<T>(T message)
    {
        if (!IsActive || _serverEndPoint == null) return;

        try
        {
            var payload = _serializer.Serialize(message);
            var packet = new UdpPacket(
                Guid.NewGuid(),
                _sequencer.GetNextSequenceNumber(),
                payload,
                RequiresAck: true // Tasdiq talab qilamiz
            );

            // Paketni "Kutilayotganlar" ro'yxatiga qo'shamiz
            _pendingAcks.TryAdd(packet.PacketId, (packet, DateTimeOffset.Now, 0));

            var finalData = _serializer.Serialize(packet);
            await SendRawAsync(finalData, _serverEndPoint!);
        }
        catch (Exception ex)
        {
            OnError?.Invoke(this, ex);
        }
    }

    /// <summary>
    /// Runs the receive loop to handle incoming messages.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task ReceiveLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var result = await Socket.ReceiveAsync(cancellationToken);

                // Deserialize the incoming packet as UdpPacket
                var packet = _serializer.Deserialize<UdpPacket>(result.Buffer);
                if (packet == null) continue;

                // Kelgan payloadni CommandPackage sifatida ochamiz
                var command = _serializer.Deserialize<CommandPackage>(packet.Payload);
                if (command == null) continue;

                // AGAR BU ACK BO'LSA:
                if (command.Type == CommandType.Ack)
                {
                    if (Guid.TryParse(command.Content, out Guid confirmedId))
                    {
                        _pendingAcks.TryRemove(confirmedId, out _);
                        Logger.Log(LogLevel.Info, $"Paket tasdiqlandi: {confirmedId}");
                    }
                    continue;
                }

                // Process messages in order using the sequencer and forward to MessageReceived
                var orderedMessages = _sequencer.ProcessInOrder(
                    packet.SequenceNumber,
                    packet.Payload);

                foreach (var msg in orderedMessages)
                {
                    MessageReceived?.Invoke(this, msg);
                }
            }
            catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
            {
                OnError?.Invoke(this, ex);
            }
        }
    }

    private async Task RetryLoop(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            var now = DateTimeOffset.Now;
            foreach (var (id, entry) in _pendingAcks)
            {
                if ((now - entry.SentTime).TotalMilliseconds > 500) // 500ms kutish
                {
                    if (entry.RetryCount < 3) // Maksimal 3 marta urinish
                    {
                        // Qayta yuborish
                        var finalData = _serializer.Serialize(entry.Packet);
                        await SendRawAsync(finalData, _serverEndPoint!);

                        // Ma'lumotni yangilash
                        _pendingAcks[id] = (entry.Packet, now, entry.RetryCount + 1);
                    }
                    else
                    {
                        _pendingAcks.TryRemove(id, out _);
                        Logger.Log(LogLevel.Error, $"Paket yo'qoldi (PacketId: {id})");
                    }
                }
            }
            await Task.Delay(100, ct);
        }
    }

    /// <summary>
    /// Runs the heartbeat loop to maintain connection.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token to stop the loop.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    private async Task HeartbeatLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            // Send PING command wrapped in UdpPacket
            var pingCommand = new CommandPackage(CommandType.SendMessage, "SYSTEM", "PING");
            await SendReliableAsync(pingCommand);

            await Task.Delay(5000, cancellationToken);
        }
    }

    /// <summary>
    /// Disconnects the UDP client from the remote endpoint.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
    public Task DisconnectAsync()
    {
        IsActive = false;
        _cancellationTokenSource.Cancel();
        Socket.Close();
        return Task.CompletedTask;
    }

    /// <summary>
    /// Disposes the UDP client resources.
    /// </summary>
    public void Dispose()
    {
        DisconnectAsync();

        GC.SuppressFinalize(this);
    }
}
