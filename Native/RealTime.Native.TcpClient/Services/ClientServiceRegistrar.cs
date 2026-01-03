using Microsoft.Extensions.DependencyInjection;
using RealTime.Native.TcpClient.Abstractions;
using RealTime.Native.TcpClient.Core;

namespace RealTime.Native.TcpClient.Services;

/// <summary>
/// Provides methods to register client-specific services for dependency injection
/// </summary>
public static class ClientServiceRegistrar
{
    /// <summary>
    /// Registers client services with the service collection
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="options">The client options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddClientServices(this IServiceCollection services, ClientOptions? options = null)
    {
        var clientOptions = options ?? new ClientOptions();
        
        services.AddSingleton(clientOptions);
        services.AddSingleton<ITcpClient, NativeClient>();
        
        return services;
    }
}
