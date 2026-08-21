using System.ComponentModel;
using Microsoft.AspNetCore.Builder;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides access to every generated HTTP operation for one remote contract method.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class RemoteServiceMethodConventionBuilder : IEndpointConventionBuilder
{
    private readonly IReadOnlyDictionary<RemoteHttpMethod, IEndpointConventionBuilder> operations;

    /// <summary>
    /// Initializes a new instance of the <see cref="RemoteServiceMethodConventionBuilder"/> class.
    /// </summary>
    /// <param name="operations">The endpoint builders keyed by HTTP method.</param>
    public RemoteServiceMethodConventionBuilder(
        IReadOnlyDictionary<RemoteHttpMethod, IEndpointConventionBuilder> operations)
    {
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
    }

    /// <summary>
    /// Gets the endpoint convention builder for a specific HTTP method.
    /// </summary>
    /// <param name="method">The HTTP method to retrieve.</param>
    /// <returns>The endpoint convention builder for the requested HTTP operation.</returns>
    /// <exception cref="KeyNotFoundException">The contract method does not expose the requested HTTP method.</exception>
    public IEndpointConventionBuilder HttpMethod(RemoteHttpMethod method)
    {
        return operations.TryGetValue(method, out var operation)
            ? operation
            : throw new KeyNotFoundException(
                $"The remote contract method does not expose HTTP {method}. Available HTTP methods: "
                + string.Join(", ", operations.Keys.OrderBy(value => value)));
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        foreach (var operation in operations.Values)
        {
            operation.Add(convention);
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        ArgumentNullException.ThrowIfNull(finalConvention);

        foreach (var operation in operations.Values)
        {
            operation.Finally(finalConvention);
        }
    }
}
