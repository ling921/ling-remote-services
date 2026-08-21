using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ling.RemoteServices.Client;

/// <summary>
/// Configures generated remote service clients.
/// </summary>
public sealed class RemoteServiceClientOptions
{
    /// <summary>
    /// Gets or sets the JSON serializer options used for request and response bodies.
    /// </summary>
    public JsonSerializerOptions JsonSerializerOptions { get; set; } = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Adds source-generated JSON metadata used by generated client proxies.
    /// </summary>
    /// <param name="context">The serializer context containing every JSON request and response type.</param>
    /// <returns>The current options instance.</returns>
    /// <remarks>
    /// Native AOT applications must register a context containing all contract types that are
    /// transferred as JSON. The context is inserted before existing resolvers so its metadata
    /// takes precedence.
    /// </remarks>
    public RemoteServiceClientOptions AddJsonSerializerContext(JsonSerializerContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        JsonSerializerOptions.TypeInfoResolverChain.Insert(0, context);
        return this;
    }

    /// <summary>
    /// Gets or sets the maximum number of bytes retained from a non-Problem error response.
    /// </summary>
    public int MaximumErrorBodySize { get; set; } = 64 * 1024;

    /// <summary>
    /// Gets or sets a value indicating whether browser request streaming may be enabled when supported.
    /// </summary>
    public bool EnableBrowserRequestStreaming { get; set; }
}
