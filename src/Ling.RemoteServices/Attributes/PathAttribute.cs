namespace Ling.RemoteServices.Attributes;

/// <summary>
/// Indicates that the parameter value comes from the route's path.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class PathAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the parameter in the path.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathAttribute"/> class.
    /// </summary>
    public PathAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PathAttribute"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the parameter in the path.</param>
    public PathAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Indicates that the parameter value comes from the query string.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = false)]
public sealed class QueryAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the parameter in the query.
    /// </summary>
    public string? Name { get; init; }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class.
    /// </summary>
    public QueryAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryAttribute"/> class with the specified name.
    /// </summary>
    /// <param name="name">The name of the parameter in the query.</param>
    public QueryAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Indicates that the parameter value comes from the request header.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class HeaderAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeaderAttribute"/> class.
    /// </summary>
    /// <param name="name">The request header name.</param>
    public HeaderAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the request header name.
    /// </summary>
    public string Name { get; }
}

/// <summary>
/// Indicates that the parameter value comes from the request body.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class BodyAttribute : Attribute
{
    /// <summary>
    /// Gets the request body media type.
    /// </summary>
    public string ContentType { get; init; } = "application/json";
}

/// <summary>
/// Indicates that the parameter value comes from the request form.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = false)]
public sealed class FormAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FormAttribute"/> class.
    /// </summary>
    /// <param name="name">The form field name, or <see langword="null"/> to use the parameter name.</param>
    public FormAttribute(string? name = null)
    {
        Name = name;
    }

    /// <summary>
    /// Gets the form field name, or <see langword="null"/> to use the parameter name.
    /// </summary>
    public string? Name { get; init; }
}
