using System.Collections.Concurrent;

namespace RealTime.Native.Udp.Reliable;

/// <summary>
/// Manages acknowledgments for reliable UDP communication.
/// </summary>
public class AckManager
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pendingAcks = new();

    /// <summary>
    /// Sends a packet and waits for acknowledgment.
    /// </summary>
    /// <param name="packetId">The unique identifier for the packet.</param>
    /// <param name="sendAction">The action to send the packet.</param>
    /// <param name="timeout">The timeout period to wait for acknowledgment.</param>
    /// <returns>True if acknowledgment is received, false otherwise.</returns>
    public async Task<bool> SendWithAck(Guid packetId, Func<Task> sendAction, TimeSpan timeout)
    {
        var taskCompletionSource = new TaskCompletionSource<bool>();
        _pendingAcks[packetId] = taskCompletionSource;

        await sendAction();

        var completedTask = await Task.WhenAny(taskCompletionSource.Task, Task.Delay(timeout));

        _pendingAcks.TryRemove(packetId, out _);

        return completedTask == taskCompletionSource.Task && await taskCompletionSource.Task;
    }

    /// <summary>
    /// Processes an acknowledgment for the specified packet.
    /// </summary>
    /// <param name="packetId">The unique identifier of the packet being acknowledged.</param>
    public void ReceiveAck(Guid packetId)
    {
        if (_pendingAcks.TryGetValue(packetId, out var taskCompletionSource))
            taskCompletionSource.TrySetResult(true);
    }
}
