using System.ComponentModel;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides access to the endpoint convention builders generated for one remote service.
/// </summary>
/// <typeparam name="TService">The remote service contract type.</typeparam>
/// <remarks>
/// Initializes a new instance of the
/// <see cref="RemoteServiceEndpointConventionBuilder{TService}"/> class.
/// </remarks>
/// <param name="group">The route group that contains the service endpoints.</param>
/// <param name="operations">The endpoint builders keyed by contract method name.</param>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RemoteServiceEndpointConventionBuilder<TService>(
    RouteGroupBuilder group,
    IReadOnlyDictionary<string, RemoteServiceMethodConventionBuilder> operations) : IEndpointConventionBuilder
    where TService : class
{
    private readonly IReadOnlyDictionary<string, RemoteServiceMethodConventionBuilder> _operations = operations ?? throw new ArgumentNullException(nameof(operations));

    /// <summary>
    /// Gets the route group that contains all endpoints for the remote service.
    /// </summary>
    public RouteGroupBuilder Group { get; } = group ?? throw new ArgumentNullException(nameof(group));

    /// <summary>
    /// Gets the endpoint convention builder for a remote contract method.
    /// </summary>
    /// <param name="methodName">
    /// The contract method name, normally supplied with <see langword="nameof"/>.
    /// </param>
    /// <returns>A convention builder that applies conventions to all HTTP operations for the method.</returns>
    /// <exception cref="KeyNotFoundException">
    /// No generated remote operation has the supplied method name.
    /// </exception>
    public RemoteServiceMethodConventionBuilder Operation(string methodName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(methodName);

        return _operations.TryGetValue(methodName, out var operation)
            ? operation
            : throw new KeyNotFoundException(
                $"Remote service '{typeof(TService).FullName}' does not contain a generated "
                + $"operation named '{methodName}'. Available operations: "
                + string.Join(", ", _operations.Keys.OrderBy(name => name, StringComparer.Ordinal)));
    }

    /// <summary>
    /// Gets the endpoint convention builder for one HTTP operation of a remote contract method.
    /// </summary>
    /// <param name="methodName">The contract method name, normally supplied with <see langword="nameof"/>.</param>
    /// <param name="httpMethod">The HTTP method to retrieve.</param>
    /// <returns>The endpoint convention builder for the requested HTTP operation.</returns>
    public IEndpointConventionBuilder Operation(string methodName, RemoteHttpMethod httpMethod)
    {
        return Operation(methodName).HttpMethod(httpMethod);
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        ((IEndpointConventionBuilder)Group).Add(convention);
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        ((IEndpointConventionBuilder)Group).Finally(finalConvention);
    }
}
