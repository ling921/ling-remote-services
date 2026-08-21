using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Text.RegularExpressions;
using Ling.RemoteServices.Exceptions;
using Ling.RemoteServices.Models;

namespace Ling.RemoteServices.Client;

/// <summary>
/// Provides infrastructure used by generated remote service clients.
/// </summary>
[EditorBrowsable(EditorBrowsableState.Never)]
public static partial class RemoteServiceClientRuntime
{
    [GeneratedRegex("(?<prefix>[./-]?)\\{(?<catch>\\*{0,2})(?<name>[A-Za-z_][A-Za-z0-9_]*)(?:[^}=?]*)?(?<optional>\\?)?\\}", RegexOptions.Compiled)]
    private static partial Regex RouteParameterRegex();

    /// <summary>
    /// Substitutes route values into an outbound route template.
    /// </summary>
    /// <param name="template">The route template.</param>
    /// <param name="values">The route values keyed by token name.</param>
    /// <returns>The encoded request path.</returns>
    public static string BuildPath(string template, IReadOnlyDictionary<string, object?> values)
    {
        var path = RouteParameterRegex().Replace(template, match =>
        {
            var name = match.Groups["name"].Value;
            if (!values.TryGetValue(name, out var value) || value is null)
            {
                return match.Groups["optional"].Success
                    ? string.Empty
                    : throw new ArgumentNullException(name, $"Required route value '{name}' is null.");
            }

            var preserveSlashes = match.Groups["catch"].Value == "**";
            return match.Groups["prefix"].Value + EncodePath(value, preserveSlashes);
        });

        return path.Replace("//", "/", StringComparison.Ordinal).TrimEnd('/');
    }

    /// <summary>
    /// Formats a protocol scalar using invariant culture.
    /// </summary>
    /// <param name="value">The value to format.</param>
    /// <returns>The invariant string representation.</returns>
    public static string Format(object? value) => value switch
    {
        DateTime dateTime => dateTime.ToString("O", CultureInfo.InvariantCulture),
        DateTimeOffset dateTimeOffset => dateTimeOffset.ToString("O", CultureInfo.InvariantCulture),
        DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
        TimeOnly time => time.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture),
        TimeSpan duration => duration.ToString("c", CultureInfo.InvariantCulture),
        Guid identifier => identifier.ToString("D"),
        bool boolean => boolean ? "true" : "false",
        IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty,
        _ => value?.ToString() ?? string.Empty
    };

    /// <summary>
    /// Encodes a value for use in a route path.
    /// </summary>
    /// <param name="value">The route value.</param>
    /// <param name="preserveSlashes">Whether path separators are preserved for a double catch-all token.</param>
    /// <returns>The encoded route value.</returns>
    public static string EncodePath(object value, bool preserveSlashes = false)
    {
        var text = Format(value);
        return preserveSlashes
            ? string.Join("/", text.Split('/').Select(Uri.EscapeDataString))
            : Uri.EscapeDataString(text);
    }

    /// <summary>
    /// Adds one or more encoded query values to a request query collection.
    /// </summary>
    /// <param name="parts">The query collection to update.</param>
    /// <param name="name">The query parameter name.</param>
    /// <param name="value">The scalar or enumerable value.</param>
    public static void AddQuery(List<string> parts, string name, object? value)
    {
        if (value is null)
        {
            return;
        }

        if (value is IEnumerable values and not string)
        {
            foreach (var item in values)
            {
                AddQuery(parts, name, item);
            }

            return;
        }

        parts.Add(Uri.EscapeDataString(name) + "=" + Uri.EscapeDataString(Format(value)));
    }

    /// <summary>
    /// Resolves source-generated JSON metadata for a contract type.
    /// </summary>
    /// <typeparam name="T">The contract type to resolve.</typeparam>
    /// <param name="options">The configured serializer options.</param>
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
    /// Throws a <see cref="RemoteCallException"/> for a non-successful HTTP response.
    /// </summary>
    /// <param name="response">The response to inspect.</param>
    /// <param name="operation">The remote operation identifier.</param>
    /// <param name="options">The client options.</param>
    /// <param name="cancellationToken">A token that cancels reading the response.</param>
    /// <returns>A task that completes only when response processing has finished.</returns>
    /// <exception cref="RemoteCallException">The response represents a remote call failure.</exception>
    public static async Task ThrowForFailureAsync(
        HttpResponseMessage response,
        string operation,
        RemoteServiceClientOptions options,
        CancellationToken cancellationToken)
    {
        var headers = Headers(response);
        RemoteProblemDetails? problem = null;
        string? excerpt = null;

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (string.Equals(mediaType, "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                problem = await response.Content.ReadFromJsonAsync(
                    RemoteServiceClientJsonSerializerContext.Default.RemoteProblemDetails,
                    cancellationToken);
            }
            catch (JsonException)
            {
                // Preserve invalid Problem Details as a bounded response excerpt below.
            }
        }

        if (problem is null)
        {
            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var excerptLength = Math.Min(bytes.Length, options.MaximumErrorBodySize);
            excerpt = Encoding.UTF8.GetString(bytes, 0, excerptLength);
        }

        throw new RemoteCallException((int)response.StatusCode, operation, problem, headers, excerpt);
    }

    /// <summary>
    /// Copies response and content headers into a case-insensitive dictionary.
    /// </summary>
    /// <param name="response">The response whose headers are copied.</param>
    /// <returns>A dictionary containing the response headers.</returns>
    public static IReadOnlyDictionary<string, string[]> Headers(HttpResponseMessage response) => response.Headers
        .Concat(response.Content.Headers)
        .ToDictionary(
            header => header.Key,
            header => header.Value.ToArray(),
            StringComparer.OrdinalIgnoreCase);

    private static InvalidOperationException CreateMissingJsonMetadataException<T>(
        Exception? innerException = null)
    {
        return new InvalidOperationException(
            $"No JSON metadata is available for '{typeof(T)}'. Register a JsonSerializerContext "
            + "with RemoteServiceClientOptions.AddJsonSerializerContext when using Native AOT.",
            innerException);
    }
}
