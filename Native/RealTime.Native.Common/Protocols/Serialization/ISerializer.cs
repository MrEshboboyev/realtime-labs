namespace RealTime.Native.Common.Protocols.Serialization;

/// <summary>
/// Defines the contract for serialization and deserialization operations
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Serializes an object to byte array
    /// </summary>
    /// <typeparam name="T">The type of object to serialize</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The serialized byte array</returns>
    byte[] Serialize<T>(T obj);
    
    /// <summary>
    /// Deserializes a byte array to an object
    /// </summary>
    /// <typeparam name="T">The type of object to deserialize to</typeparam>
    /// <param name="data">The byte array to deserialize</param>
    /// <returns>The deserialized object or null if deserialization fails</returns>
    T? Deserialize<T>(byte[] data);
}
