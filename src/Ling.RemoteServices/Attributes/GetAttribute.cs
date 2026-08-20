using System.Diagnostics.CodeAnalysis;

namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Defines a route for a GET request.
/// </summary>
/// <param name="route">The route for the GET request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GetAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the route for the GET request.
    /// </summary>
    public string? Route { get; } = route;
}

/// <summary>
/// Defines a route for a POST request.
/// </summary>
/// <param name="route">The route for the POST request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PostAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the route for the POST request.
    /// </summary>
    public string? Route { get; } = route;
}

/// <summary>
/// Defines a route for a PUT request.
/// </summary>
/// <param name="route">The route for the PUT request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PutAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the route for the PUT request.
    /// </summary>
    public string? Route { get; } = route;
}

/// <summary>
/// Defines a route for a DELETE request.
/// </summary>
/// <param name="route">The route for the DELETE request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class DELETEAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the route for the DELETE request.
    /// </summary>
    public string? Route { get; } = route;
}

/// <summary>
/// Defines a route for a PATCH request.
/// </summary>
/// <param name="route">The route for the PATCH request.</param>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class PatchAttribute([StringSyntax("Route")] string route) : Attribute
{
    /// <summary>
    /// Gets the route for the PATCH request.
    /// </summary>
    public string? Route { get; } = route;
}
