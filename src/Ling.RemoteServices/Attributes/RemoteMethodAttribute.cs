using System.Diagnostics.CodeAnalysis;

namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Defines the common HTTP metadata for a remote service method.
/// </summary>
/// <param name="route">The route relative to the remote service route.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public abstract class RemoteMethodAttribute(string route) : Attribute
{
    /// <summary>
    /// Gets the route relative to the remote service route.
    /// </summary>
    [StringSyntax("Route")]
    public string Route { get; } = route;

    /// <summary>
    /// Gets the expected successful HTTP status code, or <see langword="null"/> to use the default.
    /// </summary>
    public int? SuccessStatusCode { get; init; }

    /// <summary>
    /// Gets the response media type, or <see langword="null"/> to infer it from the return type.
    /// </summary>
    public string? ResponseContentType { get; init; }

    /// <summary>
    /// Gets or sets a value indicating whether this operation is the default HTTP method
    /// used by the generated client proxy.
    /// </summary>
    /// <remarks>
    /// A method with one HTTP operation uses that operation automatically. A method with
    /// multiple HTTP operations must mark exactly one operation as the client default.
    /// </remarks>
    public bool IsClientDefault { get; init; }
}

/// <summary>
/// Defines a GET operation.
/// </summary>
/// <param name="route">The route relative to the remote service route.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class GetAttribute([StringSyntax("Route")] string route = "") : RemoteMethodAttribute(route) { }

/// <summary>
/// Defines a route for a POST request.
/// </summary>
/// <param name="route">The route for the POST request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PostAttribute([StringSyntax("Route")] string route = "") : RemoteMethodAttribute(route) { }

/// <summary>
/// Defines a route for a PUT request.
/// </summary>
/// <param name="route">The route for the PUT request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PutAttribute([StringSyntax("Route")] string route = "") : RemoteMethodAttribute(route) { }

/// <summary>
/// Defines a route for a DELETE request.
/// </summary>
/// <param name="route">The route for the DELETE request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class DeleteAttribute([StringSyntax("Route")] string route = "") : RemoteMethodAttribute(route) { }

/// <summary>
/// Defines a route for a PATCH request.
/// </summary>
/// <param name="route">The route for the PATCH request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
public sealed class PatchAttribute([StringSyntax("Route")] string route = "") : RemoteMethodAttribute(route) { }
