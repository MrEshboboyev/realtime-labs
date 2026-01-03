using Microsoft.Extensions.DependencyInjection;
using RealTime.Native.TcpServer.Abstractions;
using RealTime.Native.TcpServer.Core;

namespace RealTime.Native.TcpServer.Services;

/// <summary>
/// Provides methods to register server-specific services for dependency injection
/// </summary>
public static class ServerServiceRegistrar
{
    /// <summary>
    /// Registers server services with the service collection
    /// </summary>
    /// <param name="services">The service collection to register services with</param>
    /// <param name="options">The server options</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddServerServices(this IServiceCollection services, ServerOptions? options = null)
    {
        var serverOptions = options ?? new ServerOptions();
        
        services.AddSingleton(serverOptions);
        services.AddSingleton<ConnectionManager>();
        services.AddSingleton<IServer, TcpServerListener>();
        
        return services;
    }
}
