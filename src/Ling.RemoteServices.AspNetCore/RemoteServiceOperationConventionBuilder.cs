using System.ComponentModel;
using Microsoft.AspNetCore.Builder;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>Configures one generated HTTP operation.</summary>
public sealed class RemoteServiceOperationConventionBuilder(
    RemoteHttpMethod method,
    IEndpointConventionBuilder endpoint)
    : RemoteServiceConventionBuilder<RemoteServiceOperationConventionBuilder>
{
    /// <summary>Gets the HTTP method represented by this builder.</summary>
    public RemoteHttpMethod Method { get; } = method;

    /// <summary>Gets the underlying ASP.NET Core endpoint builder.</summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public IEndpointConventionBuilder Endpoint { get; } = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

    /// <inheritdoc />
    public override void Add(Action<EndpointBuilder> convention)
    {
        ArgumentNullException.ThrowIfNull(convention);
        Endpoint.Add(convention);
    }

    /// <inheritdoc />
    public override void Finally(Action<EndpointBuilder> finalConvention)
    {
        ArgumentNullException.ThrowIfNull(finalConvention);
        Endpoint.Finally(finalConvention);
    }

    /// <inheritdoc />
    protected override IEnumerable<IEndpointConventionBuilder> GetEndpointBuilders()
    {
        yield return Endpoint;
    }
}
