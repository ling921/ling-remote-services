namespace Ling.RemoteServices.Attributes;

/// <summary>Base class for endpoint metadata that can target one HTTP operation.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class RemoteEndpointMetadataAttribute : Attribute
{
    /// <summary>Gets or sets the HTTP operation selected by this metadata. Omit it to target all operations.</summary>
    public RemoteHttpMethod HttpMethod { get; set; }
}

/// <summary>Documents a response produced by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteProducesAttribute<TResponse>(
    int statusCode = 200,
    string? contentType = null,
    params string[] additionalContentTypes) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the HTTP response status code.</summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>Gets the primary response media type.</summary>
    public string? ContentType { get; } = contentType;
    /// <summary>Gets additional response media types.</summary>
    public string[] AdditionalContentTypes { get; } = additionalContentTypes;
}

/// <summary>Documents a response produced by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteProducesAttribute(
    int statusCode = 200,
    Type? responseType = null,
    string? contentType = null,
    params string[] additionalContentTypes) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the HTTP response status code.</summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>Gets the response CLR type.</summary>
    public Type? ResponseType { get; } = responseType;
    /// <summary>Gets the primary response media type.</summary>
    public string? ContentType { get; } = contentType;
    /// <summary>Gets additional response media types.</summary>
    public string[] AdditionalContentTypes { get; } = additionalContentTypes;
}

/// <summary>Documents a problem response produced by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteProducesProblemAttribute(
    int statusCode = 500,
    string? contentType = null) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the HTTP response status code.</summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>Gets the response media type.</summary>
    public string? ContentType { get; } = contentType;
}

/// <summary>Documents a validation problem response produced by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteProducesValidationProblemAttribute(
    int statusCode = 400,
    string? contentType = null) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the HTTP response status code.</summary>
    public int StatusCode { get; } = statusCode;
    /// <summary>Gets the response media type.</summary>
    public string? ContentType { get; } = contentType;
}

/// <summary>Documents the request body accepted by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteAcceptsAttribute<TRequest>(
    string contentType,
    bool isOptional = false,
    params string[] additionalContentTypes) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the request media type.</summary>
    public string ContentType { get; } = contentType;
    /// <summary>Gets whether the request body is optional.</summary>
    public bool IsOptional { get; } = isOptional;
    /// <summary>Gets additional request media types.</summary>
    public string[] AdditionalContentTypes { get; } = additionalContentTypes;
}

/// <summary>Documents the request body accepted by a remote endpoint.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteAcceptsAttribute(
    Type requestType,
    string contentType,
    bool isOptional = false,
    params string[] additionalContentTypes) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the request CLR type.</summary>
    public Type RequestType { get; } = requestType;
    /// <summary>Gets the request media type.</summary>
    public string ContentType { get; } = contentType;
    /// <summary>Gets whether the request body is optional.</summary>
    public bool IsOptional { get; } = isOptional;
    /// <summary>Gets additional request media types.</summary>
    public string[] AdditionalContentTypes { get; } = additionalContentTypes;
}

/// <summary>Adds OpenAPI tags to remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteTagsAttribute(params string[] tags) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the OpenAPI tag names.</summary>
    public string[] Tags { get; } = tags;
}

/// <summary>Sets the OpenAPI summary for remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteSummaryAttribute(string summary) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the OpenAPI summary.</summary>
    public string Summary { get; } = summary;
}

/// <summary>Sets the OpenAPI description for remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteDescriptionAttribute(string description) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the OpenAPI description.</summary>
    public string Description { get; } = description;
}

/// <summary>Controls whether remote endpoints are included in OpenAPI descriptions.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteExcludeFromDescriptionAttribute : RemoteEndpointMetadataAttribute
{
}

/// <summary>Restricts remote endpoints to the specified hosts.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteHostAttribute(params string[] hosts) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the allowed host names.</summary>
    public string[] Hosts { get; } = hosts;
}

/// <summary>Sets the route order for remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteOrderAttribute(int order) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the endpoint route order.</summary>
    public int Order { get; } = order;
}

/// <summary>Sets the display name for remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteDisplayNameAttribute(string displayName) : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets the endpoint display name.</summary>
    public string DisplayName { get; } = displayName;
}

/// <summary>Sets a unique route endpoint name. Apply this to a method with one HTTP operation.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RemoteEndpointNameAttribute(string name) : Attribute
{
    /// <summary>Gets the unique endpoint name.</summary>
    public string Name { get; } = name;
}

/// <summary>Executes a remote endpoint during routing and skips later middleware.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteShortCircuitAttribute : RemoteEndpointMetadataAttribute
{
    /// <summary>Initializes short-circuiting with the endpoint's normal response status.</summary>
    public RemoteShortCircuitAttribute()
    {
    }

    /// <summary>Initializes short-circuiting with a response status override.</summary>
    /// <param name="statusCode">The response status code from 200 through 599.</param>
    public RemoteShortCircuitAttribute(int statusCode)
    {
        if (statusCode is < 200 or > 599)
        {
            throw new ArgumentOutOfRangeException(nameof(statusCode), statusCode, "Status code must be from 200 through 599.");
        }

        StatusCode = statusCode;
    }

    /// <summary>Gets the response status override, or zero to use the endpoint's normal response.</summary>
    public int StatusCode { get; }
}

/// <summary>Disables antiforgery validation for the selected remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteDisableAntiforgeryAttribute : RemoteEndpointMetadataAttribute
{
}

/// <summary>Disables .NET 10 Minimal API validation for the selected remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteDisableValidationAttribute : RemoteEndpointMetadataAttribute
{
}

/// <summary>Controls cookie authentication redirects for .NET 10 remote endpoints.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteAllowCookieRedirectAttribute : RemoteEndpointMetadataAttribute
{
}

/// <summary>Configures form limits for remote endpoints that read forms.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteFormOptionsAttribute : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets or sets whether the request body is buffered before form parsing.</summary>
    public bool BufferBody { get; set; }
    /// <summary>Gets or sets the memory buffer threshold, or -1 to leave the framework default.</summary>
    public int MemoryBufferThreshold { get; set; } = -1;
    /// <summary>Gets or sets the body buffer limit, or -1 to leave the framework default.</summary>
    public long BufferBodyLengthLimit { get; set; } = -1;
    /// <summary>Gets or sets the maximum number of form values, or -1 to leave the framework default.</summary>
    public int ValueCountLimit { get; set; } = -1;
    /// <summary>Gets or sets the maximum form key length, or -1 to leave the framework default.</summary>
    public int KeyLengthLimit { get; set; } = -1;
    /// <summary>Gets or sets the maximum form value length, or -1 to leave the framework default.</summary>
    public int ValueLengthLimit { get; set; } = -1;
    /// <summary>Gets or sets the multipart boundary length limit, or -1 to leave the framework default.</summary>
    public int MultipartBoundaryLengthLimit { get; set; } = -1;
    /// <summary>Gets or sets the multipart headers count limit, or -1 to leave the framework default.</summary>
    public int MultipartHeadersCountLimit { get; set; } = -1;
    /// <summary>Gets or sets the multipart headers length limit, or -1 to leave the framework default.</summary>
    public int MultipartHeadersLengthLimit { get; set; } = -1;
    /// <summary>Gets or sets the multipart body length limit, or -1 to leave the framework default.</summary>
    public long MultipartBodyLengthLimit { get; set; } = -1;
}

/// <summary>Configures limits for binding complex objects and collections from form data.</summary>
[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RemoteFormMappingOptionsAttribute : RemoteEndpointMetadataAttribute
{
    /// <summary>Gets or sets the maximum number of form collection elements, or -1 for the framework default.</summary>
    public int MaxCollectionSize { get; set; } = -1;

    /// <summary>Gets or sets the maximum recursive form mapping depth, or -1 for the framework default.</summary>
    public int MaxRecursionDepth { get; set; } = -1;

    /// <summary>Gets or sets the maximum form mapping key size, or -1 for the framework default.</summary>
    public int MaxKeySize { get; set; } = -1;
}
