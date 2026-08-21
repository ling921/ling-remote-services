using Microsoft.AspNetCore.Builder;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides a generated endpoint and its contract identity to a custom endpoint policy.
/// </summary>
public sealed class RemoteServiceEndpointPolicyContext : IEndpointConventionBuilder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteServiceEndpointPolicyContext"/> class.
    /// </summary>
    /// <param name="serviceType">The remote service contract type.</param>
    /// <param name="methodName">The remote contract method name.</param>
    /// <param name="endpoint">The generated route handler builder.</param>
    public RemoteServiceEndpointPolicyContext(
        Type serviceType,
        string methodName,
        IEndpointConventionBuilder endpoint)
    {
        ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);
        MethodName = methodName;
        Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
    }

    /// <summary>
    /// Gets the remote service contract type.
    /// </summary>
    public Type ServiceType { get; }

    /// <summary>
    /// Gets the remote contract method name.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Gets the generated endpoint convention builder.
    /// </summary>
    public IEndpointConventionBuilder Endpoint { get; }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        Endpoint.Add(convention);
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        Endpoint.Finally(finalConvention);
    }
}
