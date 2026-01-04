using System.Collections.Concurrent;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;

namespace RealTime.Native.Common.Protocols.Framing;

/// <summary>
/// Implements length-prefixed framing for reliable message transmission
/// Each message is prefixed with a 4-byte length indicator
/// </summary>
public class LengthPrefixedFrame(
    SharedLogger? logger = null
) : IFrameHandler
{
    // Stores incomplete data for each client
    private readonly ConcurrentDictionary<Guid, List<byte>> _buffers = new();

    /// <summary>
    /// Wraps data with a length prefix
    /// </summary>
    /// <param name="data">The data to wrap</param>
    /// <returns>Frame with 4-byte length prefix prepended</returns>
    public byte[] Wrap(byte[] data)
    {
        try
        {
            byte[] lengthPrefix = BitConverter.GetBytes(data.Length); // 4 bytes
            byte[] frame = new byte[lengthPrefix.Length + data.Length];

            Buffer.BlockCopy(lengthPrefix, 0, frame, 0, lengthPrefix.Length);
            Buffer.BlockCopy(data, 0, frame, lengthPrefix.Length, data.Length);

            return frame;
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error wrapping data with length prefix", ex);
            throw;
        }
    }

    /// <summary>
    /// Unwraps received data, extracting complete messages from the buffer
    /// </summary>
    /// <param name="receivedData">Newly received data</param>
    /// <param name="connectionId">The connection identifier</param>
    /// <returns>Enumeration of complete messages</returns>
    public IEnumerable<byte[]> Unwrap(byte[] receivedData, Guid connectionId)
    {
        var messages = new List<byte[]>();
        
        try
        {
            var connectionBuffer = _buffers.GetOrAdd(connectionId, _ => new List<byte>());
            connectionBuffer.AddRange(receivedData);

            while (connectionBuffer.Count >= sizeof(int)) // 4 bytes for length prefix
            {
                // Get the message length from first 4 bytes
                byte[] lengthBytes = [.. connectionBuffer.GetRange(0, sizeof(int))];
                int messageLength = BitConverter.ToInt32(lengthBytes, 0);
                
                if (messageLength < 0 || messageLength > 1024 * 1024) // 1MB max message size
                {
                    logger?.Log(LogLevel.Error, $"Invalid message length: {messageLength} for connection {connectionId}");
                    // Clear the buffer to prevent corruption
                    connectionBuffer.Clear();
                    break; // Stop processing for this connection
                }

                // Check if we have a complete message
                if (connectionBuffer.Count >= sizeof(int) + messageLength)
                {
                    byte[] message = [.. connectionBuffer.GetRange(sizeof(int), messageLength)];
                    connectionBuffer.RemoveRange(0, sizeof(int) + messageLength);
                    messages.Add(message);
                }
                else
                {
                    // Message is incomplete, wait for more data
                    break;
                }
            }
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error unwrapping data for connection {connectionId}", ex);
            throw;
        }
        
        return messages;
    }
    
    /// <summary>
    /// Clears the buffer for a specific connection
    /// </summary>
    /// <param name="connectionId">The connection identifier</param>
    public void ClearBufferForConnection(Guid connectionId)
    {
        _buffers.TryRemove(connectionId, out _);
    }
    
    /// <summary>
    /// Clears all connection buffers
    /// </summary>
    public void ClearAllBuffers()
    {
        _buffers.Clear();
    }
}
