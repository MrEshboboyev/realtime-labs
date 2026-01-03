using Microsoft.Extensions.DependencyInjection;
using RealTime.Native.Common.Infrastructure;
using RealTime.Native.Common.Protocols.Framing;
using RealTime.Native.Common.Protocols.Serialization;

namespace RealTime.Native.Common.Services;

/// <summary>
/// Provides methods to register common services for dependency injection
/// </summary>
public static class ServiceRegistrar
{
    /// <summary>
    /// Registers common services with the service collection
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="loggerOwner">The owner name for the logger</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCommonServices(this IServiceCollection services, string loggerOwner = "APP")
    {
        services.AddSingleton<SharedLogger>(provider => new SharedLogger(loggerOwner));
        services.AddSingleton<NetworkBufferPool>();
        services.AddSingleton<IFrameHandler, LengthPrefixedFrame>();
        services.AddSingleton<ISerializer, BinarySerializer>();
        
        return services;
    }
    
    /// <summary>
    /// Registers JSON serializer as the default serializer
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="loggerOwner">The owner name for the logger</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddJsonSerialization(this IServiceCollection services, string loggerOwner = "APP")
    {
        services.AddSingleton<SharedLogger>(provider => new SharedLogger(loggerOwner));
        services.AddSingleton<NetworkBufferPool>();
        services.AddSingleton<IFrameHandler, LengthPrefixedFrame>();
        services.AddSingleton<ISerializer, JsonSerializerImpl>();
        
        return services;
    }
    
    /// <summary>
    /// Registers custom serializers
    /// </summary>
    /// <typeparam name="TSerializer">The serializer type to register</typeparam>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="loggerOwner">The owner name for the logger</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddCustomSerialization<TSerializer>(this IServiceCollection services, string loggerOwner = "APP") 
        where TSerializer : class, ISerializer
    {
        services.AddSingleton<SharedLogger>(provider => new SharedLogger(loggerOwner));
        services.AddSingleton<NetworkBufferPool>();
        services.AddSingleton<IFrameHandler, LengthPrefixedFrame>();
        services.AddSingleton<ISerializer, TSerializer>();
        
        return services;
    }
}
