using Microsoft.AspNetCore.Builder;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides access to every generated HTTP operation for one remote contract method.
/// </summary>
public sealed class RemoteServiceMethodConventionBuilder
    : RemoteServiceConventionBuilder<RemoteServiceMethodConventionBuilder>
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
    public RemoteServiceOperationConventionBuilder HttpMethod(RemoteHttpMethod method)
    {
        return operations.TryGetValue(method, out var operation)
            ? new RemoteServiceOperationConventionBuilder(method, operation)
            : throw new KeyNotFoundException(
                $"The remote contract method does not expose HTTP {method}. Available HTTP methods: "
                + string.Join(", ", operations.Keys.OrderBy(value => value)));
    }

    /// <inheritdoc />
    public override void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);

        foreach (var operation in operations.Values)
        {
            operation.Add(convention);
        }
    }

    /// <inheritdoc />
    public override void Finally(Action<EndpointBuilder> finalConvention)
    {
        ArgumentNullException.ThrowIfNull(finalConvention);

        foreach (var operation in operations.Values)
        {
            operation.Finally(finalConvention);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<IEndpointConventionBuilder> GetEndpointBuilders() => operations.Values;
}
