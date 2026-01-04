using System.Collections.Concurrent;

namespace RealTime.Native.Udp.Reliable;

public class AckManager
{
    private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> _pendingAcks = new();

    public async Task<bool> SendWithAck(Guid packetId, Func<Task> sendAction, TimeSpan timeout)
    {
        var tcs = new TaskCompletionSource<bool>();
        _pendingAcks[packetId] = tcs;

        await sendAction();

        var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(timeout));

        _pendingAcks.TryRemove(packetId, out _);

        return completedTask == tcs.Task && await tcs.Task;
    }

    public void ReceiveAck(Guid packetId)
    {
        if (_pendingAcks.TryGetValue(packetId, out var tcs))
            tcs.TrySetResult(true);
    }
}
