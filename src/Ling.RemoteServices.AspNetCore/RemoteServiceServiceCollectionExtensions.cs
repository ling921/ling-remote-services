using Microsoft.Extensions.DependencyInjection;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Registers ASP.NET Core services used by remote service endpoint policies.
/// </summary>
public static class RemoteServiceServiceCollectionExtensions
{
    /// <summary>
    /// Registers host-defined remote service endpoint policies.
    /// </summary>
    /// <param name="services">The application service collection.</param>
    /// <param name="configure">An optional endpoint policy configuration callback.</param>
    /// <returns>The application service collection.</returns>
    public static IServiceCollection AddRemoteServices(
        this IServiceCollection services,
        Action<RemoteServiceOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<RemoteServiceOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        return services;
    }
}
