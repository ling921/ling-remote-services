using Microsoft.CodeAnalysis;

namespace Ling.RemoteServices.Generators;

internal enum EndpointMetadataKind
{
    Produces,
    Accepts,
    Tags,
    Summary,
    Description,
    ExcludeFromDescription,
    Host,
    Order,
    DisplayName,
    EndpointName,
    ShortCircuit,
    DisableAntiforgery,
    DisableValidation,
    AllowCookieRedirect,
    FormOptions,
    FormMappingOptions
}

internal sealed record EndpointMetadataModel(
    EndpointMetadataKind Kind,
    ITypeSymbol? Type,
    int? StatusCode,
    string? ContentType,
    IReadOnlyList<string> ContentTypes,
    IReadOnlyList<string> Values,
    IReadOnlyDictionary<string, object?> NamedValues,
    string? Text,
    int? Number,
    bool? Boolean,
    int? HttpMethod,
    bool SelectsHttpMethod);
