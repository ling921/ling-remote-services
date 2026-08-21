using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Ling.RemoteServices.Exceptions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Ling.RemoteServices.AspNetCore;

/// <summary>
/// Provides infrastructure used by generated remote service endpoints.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class RemoteServiceServerRuntime
{
    /// <summary>
    /// Returns a required scalar request value.
    /// </summary>
    /// <param name="value">The value supplied by the request.</param>
    /// <param name="name">The protocol parameter name.</param>
    /// <returns>The non-empty request value.</returns>
    /// <exception cref="BadHttpRequestException">The required value is missing.</exception>
    public static string RequireValue(string? value, string name)
    {
        return value ?? throw new BadHttpRequestException(
            $"Required request value '{name}' was not provided.",
            StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Gets a route value without using request delegate reflection.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="name">The route parameter name.</param>
    /// <returns>The invariant route value, or <see langword="null"/> when it is absent.</returns>
    public static string? GetRouteValue(HttpContext context, string name)
    {
        return context.Request.RouteValues.TryGetValue(name, out var value)
            ? Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)
            : null;
    }

    /// <summary>
    /// Gets the first query-string value without using request delegate reflection.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="name">The query parameter name.</param>
    /// <returns>The first query value, or <see langword="null"/> when it is absent.</returns>
    public static string? GetQueryValue(HttpContext context, string name)
    {
        return context.Request.Query.TryGetValue(name, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Gets all query-string values without using request delegate reflection.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="name">The query parameter name.</param>
    /// <returns>All values supplied for the query parameter.</returns>
    public static StringValues GetQueryValues(HttpContext context, string name)
    {
        return context.Request.Query.TryGetValue(name, out var values)
            ? values
            : StringValues.Empty;
    }

    /// <summary>
    /// Gets the first request-header value without using request delegate reflection.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="name">The request header name.</param>
    /// <returns>The first header value, or <see langword="null"/> when it is absent.</returns>
    public static string? GetHeaderValue(HttpContext context, string name)
    {
        return context.Request.Headers.TryGetValue(name, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Gets the first form value without using request delegate reflection.
    /// </summary>
    /// <param name="form">The parsed request form.</param>
    /// <param name="name">The form field name.</param>
    /// <returns>The first form value, or <see langword="null"/> when it is absent.</returns>
    public static string? GetFormValue(IFormCollection form, string name)
    {
        return form.TryGetValue(name, out var values)
            ? values.FirstOrDefault()
            : null;
    }

    /// <summary>
    /// Resolves source-generated JSON metadata for a contract response type.
    /// </summary>
    /// <typeparam name="T">The response type to resolve.</typeparam>
    /// <param name="options">The ASP.NET Core HTTP JSON options.</param>
    /// <returns>The strongly typed JSON metadata.</returns>
    /// <exception cref="InvalidOperationException">
    /// No compatible metadata was registered for <typeparamref name="T"/>.
    /// </exception>
    public static JsonTypeInfo<T> GetJsonTypeInfo<T>(JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        try
        {
            if (options.GetTypeInfo(typeof(T)) is JsonTypeInfo<T> typeInfo)
            {
                return typeInfo;
            }
        }
        catch (NotSupportedException exception)
        {
            throw CreateMissingJsonMetadataException<T>(exception);
        }

        throw CreateMissingJsonMetadataException<T>();
    }

    /// <summary>
    /// Converts an expected remote service exception to an ASP.NET Core result.
    /// </summary>
    /// <param name="exception">The service exception to convert.</param>
    /// <returns>A Problem Details result with the configured status code.</returns>
    public static IResult Problem(RemoteServiceException exception)
    {
        var problem = exception.Problem;
        return Results.Problem(
            problem?.Detail,
            problem?.Instance,
            exception.StatusCode,
            problem?.Title,
            problem?.Type,
            problem?.Extensions);
    }

    private static InvalidOperationException CreateMissingJsonMetadataException<T>(
        Exception? innerException = null)
    {
        return new InvalidOperationException(
            $"No JSON metadata is available for '{typeof(T)}'. Register a JsonSerializerContext "
            + "with ConfigureHttpJsonOptions when using Native AOT.",
            innerException);
    }
}

/// <summary>
/// Applies endpoint conventions to every endpoint generated for a remote service collection.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class CompositeEndpointConventionBuilder : IEndpointConventionBuilder
{
    private readonly IReadOnlyList<IEndpointConventionBuilder> builders;

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeEndpointConventionBuilder"/> class.
    /// </summary>
    /// <param name="builders">The endpoint convention builders to aggregate.</param>
    public CompositeEndpointConventionBuilder(IEnumerable<IEndpointConventionBuilder> builders)
    {
        this.builders = builders.ToArray();
    }

    /// <inheritdoc />
    public void Add(Action<EndpointBuilder> convention)
    {
        foreach (var builder in builders)
        {
            builder.Add(convention);
        }
    }

    /// <inheritdoc />
    public void Finally(Action<EndpointBuilder> finalConvention)
    {
        foreach (var builder in builders)
        {
            builder.Finally(finalConvention);
        }
    }
}
