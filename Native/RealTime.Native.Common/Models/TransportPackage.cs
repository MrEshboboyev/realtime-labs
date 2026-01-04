namespace RealTime.Native.Common.Models;

/// <summary>
/// Represents raw network data with its context
/// </summary>
public sealed record TransportPackage(
    /// <summary>
    /// The connection identifier
    /// </summary>
    Guid ConnectionId,
    
    /// <summary>
    /// The raw data received
    /// </summary>
    ReadOnlyMemory<byte> Data,
    
    /// <summary>
    /// The time when data was received
    /// </summary>
    DateTimeOffset ReceivedAt
);
