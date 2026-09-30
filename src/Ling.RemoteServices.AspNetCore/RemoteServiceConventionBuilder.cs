using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>Provides common typed endpoint configuration for generated remote services.</summary>
/// <typeparam name="TBuilder">The concrete remote service builder.</typeparam>
public abstract class RemoteServiceConventionBuilder<TBuilder> : IEndpointConventionBuilder
    where TBuilder : RemoteServiceConventionBuilder<TBuilder>
{
    /// <summary>Applies a convention to the endpoints represented by this builder.</summary>
    public abstract void Add(Action<EndpointBuilder> convention);

    /// <summary>Applies a final convention to the endpoints represented by this builder.</summary>
    public abstract void Finally(Action<EndpointBuilder> finalConvention);

    /// <summary>Adds a filter instance to the represented endpoints.</summary>
    public TBuilder AddEndpointFilter(IEndpointFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilter<TBuilder>((TBuilder)this, filter);
        return (TBuilder)this;
    }

    /// <summary>Adds a filter type to the represented endpoints.</summary>
    public TBuilder AddEndpointFilter<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TFilter>()
        where TFilter : IEndpointFilter
    {
        Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilter<TBuilder, TFilter>((TBuilder)this);
        return (TBuilder)this;
    }

    /// <summary>Adds a filter delegate to the represented endpoints.</summary>
    public TBuilder AddEndpointFilter(
        Func<EndpointFilterInvocationContext, EndpointFilterDelegate, ValueTask<object?>> filter)
    {
        ArgumentNullException.ThrowIfNull(filter);
        Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilter<TBuilder>((TBuilder)this, filter);
        return (TBuilder)this;
    }

    /// <summary>Adds a filter factory to the represented endpoints.</summary>
    public TBuilder AddEndpointFilterFactory(
        Func<EndpointFilterFactoryContext, EndpointFilterDelegate, EndpointFilterDelegate> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        Microsoft.AspNetCore.Http.EndpointFilterExtensions.AddEndpointFilterFactory<TBuilder>((TBuilder)this, factory);
        return (TBuilder)this;
    }

    /// <summary>Adds response metadata to the represented endpoints.</summary>
    public TBuilder Produces<TResponse>(
        int statusCode = StatusCodes.Status200OK,
        string? contentType = null,
        params string[] additionalContentTypes)
    {
        AddProduces(statusCode, typeof(TResponse), contentType, additionalContentTypes);
        return (TBuilder)this;
    }

    /// <summary>Adds response metadata to the represented endpoints.</summary>
    public TBuilder Produces(
        int statusCode = StatusCodes.Status200OK,
        Type? responseType = null,
        string? contentType = null,
        params string[] additionalContentTypes)
    {
        AddProduces(statusCode, responseType, contentType, additionalContentTypes);
        return (TBuilder)this;
    }

    /// <summary>Adds Problem Details response metadata to the represented endpoints.</summary>
    public TBuilder ProducesProblem(
        int statusCode = StatusCodes.Status500InternalServerError,
        string? contentType = null)
    {
        AddProduces(statusCode, typeof(ProblemDetails), contentType ?? "application/problem+json", []);
        return (TBuilder)this;
    }

    /// <summary>Adds validation-problem response metadata to the represented endpoints.</summary>
    public TBuilder ProducesValidationProblem(
        int statusCode = StatusCodes.Status400BadRequest,
        string? contentType = null)
    {
        AddProduces(
            statusCode,
            typeof(HttpValidationProblemDetails),
            contentType ?? "application/problem+json",
            []);
        return (TBuilder)this;
    }

    /// <summary>Adds request metadata to the represented endpoints.</summary>
    public TBuilder Accepts<TRequest>(
        string contentType,
        bool isOptional = false,
        params string[] additionalContentTypes)
    {
        AddAccepts(typeof(TRequest), contentType, isOptional, additionalContentTypes);
        return (TBuilder)this;
    }

    /// <summary>Adds request metadata to the represented endpoints.</summary>
    public TBuilder Accepts(
        Type requestType,
        string contentType,
        bool isOptional = false,
        params string[] additionalContentTypes)
    {
        AddAccepts(requestType, contentType, isOptional, additionalContentTypes);
        return (TBuilder)this;
    }

    /// <summary>Applies a named or duration-based request timeout.</summary>
    public TBuilder WithRequestTimeout(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ForEach(builder => RequestTimeoutsIEndpointConventionBuilderExtensions.WithRequestTimeout(builder, policyName));
        return (TBuilder)this;
    }

    /// <summary>Applies a duration-based request timeout.</summary>
    public TBuilder WithRequestTimeout(TimeSpan timeout)
    {
        ForEach(builder => RequestTimeoutsIEndpointConventionBuilderExtensions.WithRequestTimeout(builder, timeout));
        return (TBuilder)this;
    }

    /// <summary>Disables request timeouts for the represented endpoints.</summary>
    public TBuilder DisableRequestTimeout()
    {
        ForEach(builder => RequestTimeoutsIEndpointConventionBuilderExtensions.DisableRequestTimeout(builder));
        return (TBuilder)this;
    }

    /// <summary>Short-circuits the represented endpoints during routing.</summary>
    public TBuilder ShortCircuit(int? statusCode = null)
    {
        if (statusCode is < 200 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Status code must be from 200 through 599.");
        }

        ForEach(builder => RouteShortCircuitEndpointConventionBuilderExtensions.ShortCircuit(builder, statusCode));
        return (TBuilder)this;
    }

    /// <summary>Enumerates the ASP.NET Core builders represented by this typed builder.</summary>
    /// <returns>The underlying endpoint convention builders.</returns>
    protected abstract IEnumerable<IEndpointConventionBuilder> GetEndpointBuilders();

    private void AddProduces(int statusCode, Type? responseType, string? contentType, string[] additionalContentTypes)
    {
        if (statusCode is < 100 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Status code must be from 100 through 599.");
        }

        ArgumentNullException.ThrowIfNull(additionalContentTypes);

        string[] contentTypes;
        if (string.IsNullOrWhiteSpace(contentType))
        {
            if (responseType is null)
            {
                contentTypes = additionalContentTypes;
            }
            else
            {
                contentTypes = ["application/json", .. additionalContentTypes];
            }
        }
        else
        {
            contentTypes = [contentType, .. additionalContentTypes];
        }

        var metadata = new ProducesResponseTypeMetadata(
            statusCode,
            responseType ?? typeof(void),
            contentTypes);
        ForEach(builder => builder.WithMetadata(metadata));
    }

    private void AddAccepts(Type requestType, string contentType, bool isOptional, string[] additionalContentTypes)
    {
        ArgumentNullException.ThrowIfNull(requestType);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentNullException.ThrowIfNull(additionalContentTypes);

        var contentTypes = new string[additionalContentTypes.Length + 1];
        contentTypes[0] = contentType;
        additionalContentTypes.CopyTo(contentTypes, 1);
        var metadata = new AcceptsMetadata(contentTypes, requestType, isOptional);
        ForEach(builder => builder.WithMetadata(metadata));
    }

    private void ForEach(Action<IEndpointConventionBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(configure);
        foreach (var builder in GetEndpointBuilders())
        {
            configure(builder);
        }
    }
}
