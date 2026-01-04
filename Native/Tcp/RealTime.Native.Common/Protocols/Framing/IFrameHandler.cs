namespace RealTime.Native.Common.Protocols.Framing;

/// <summary>
/// Defines the contract for message framing and deframing
/// </summary>
public interface IFrameHandler
{
    /// <summary>
    /// Wraps data with a length prefix for reliable transmission
    /// </summary>
    /// <param name="data">The data to wrap</param>
    /// <returns>Frame with length prefix prepended</returns>
    byte[] Wrap(byte[] data);

    /// <summary>
    /// Unwraps received data, extracting complete messages from the buffer
    /// </summary>
    /// <param name="receivedData">Newly received data</param>
    /// <param name="connectionId">The connection identifier</param>
    /// <returns>Enumeration of complete messages</returns>
    IEnumerable<byte[]> Unwrap(byte[] receivedData, Guid connectionId);
}
