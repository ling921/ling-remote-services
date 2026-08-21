using System.ComponentModel;
using Microsoft.AspNetCore.Builder;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides strongly typed access to all generated remote service endpoint builders.
/// </summary>
public sealed class RemoteServiceEndpointConventionRegistry : IEndpointConventionBuilder
{
    private readonly Dictionary<Type, object> services = [];

    /// <summary>
    /// Gets the endpoint convention builder for a remote service contract.
    /// </summary>
    /// <typeparam name="TService">The remote service contract type.</typeparam>
    /// <returns>The convention builder generated for the service.</returns>
    /// <exception cref="KeyNotFoundException">
    /// The requested service was not mapped by the generated mapper.
    /// </exception>
    public RemoteServiceEndpointConventionBuilder<TService> For<TService>()
        where TService : class
    {
        return services.TryGetValue(typeof(TService), out var service)
            ? (RemoteServiceEndpointConventionBuilder<TService>)service
            : throw new KeyNotFoundException($"Remote service '{typeof(TService).FullName}' was not mapped.");
    }

    /// <summary>
    /// Adds a generated service endpoint builder to the registry.
    /// </summary>
    /// <typeparam name="TService">The remote service contract type.</typeparam>
    /// <param name="service">The generated service endpoint builder.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public void AddService<TService>(RemoteServiceEndpointConventionBuilder<TService> service)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(service);
        services.Add(typeof(TService), service);
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var service in services.Values.Cast<IEndpointConventionBuilder>())
        {
            service.Add(convention);
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        foreach (var service in services.Values.Cast<IEndpointConventionBuilder>())
        {
            service.Finally(finalConvention);
        }
    }
}
