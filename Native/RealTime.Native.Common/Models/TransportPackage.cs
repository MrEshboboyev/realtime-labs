namespace RealTime.Native.Common.Models;

/// <summary>
/// Tarmoqdan kelgan xom ma'lumot va uning konteksti
/// </summary>
public sealed record TransportPackage(
    Guid ConnectionId,
    ReadOnlyMemory<byte> Data,
    DateTimeOffset ReceivedAt
);
