using System.Text;
using System.Text.Json;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Models;

namespace RealTime.Native.Common.Protocols.Serialization;

/// <summary>
/// Provides JSON serialization using System.Text.Json
/// </summary>
public class JsonSerializerImpl(
    SharedLogger? logger = null, 
    JsonSerializerOptions? options = null
) : ISerializer
{
    public byte[] Serialize<T>(T obj)
    {
        try
        {
            string json = JsonSerializer.Serialize(obj, options);
            return Encoding.UTF8.GetBytes(json);
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error serializing object of type {typeof(T).Name} to JSON", ex);
            throw;
        }
    }

    public T? Deserialize<T>(byte[] data)
    {
        try
        {
            string json = Encoding.UTF8.GetString(data);
            return JsonSerializer.Deserialize<T>(json, options);
        }
        catch (Exception ex)
        {
            logger?.Log(LogLevel.Error, $"Error deserializing JSON to type {typeof(T).Name}", ex);
            throw;
        }
    }
}
