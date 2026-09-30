# Ling.RemoteServices.Abstractions

[Project documentation](https://github.com/ling921/ling-remote-services#readme) | [简体中文](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices/README.zh-CN.md)

`Ling.RemoteServices.Abstractions` contains the transport-independent contract surface for Ling.RemoteServices. It includes contract attributes, shared file and error models, remote exceptions, a contract analyzer, and the contract manifest source generator. It does not reference MVC or ASP.NET Core.

## Installation

Install the package in the project that owns the shared service interfaces:

```shell
dotnet add package Ling.RemoteServices.Abstractions
```

## Define a contract

```csharp
using Ling.RemoteServices.Attributes;
using Ling.RemoteServices.Models;

[RemoteService("/api/files")]
public interface IFileService
{
    [Get("{id}")]
    Task<FileInfoDto> GetAsync(
        [Path] Guid id,
        [Query] bool includeMetadata = false,
        CancellationToken cancellationToken = default);

    [Post("upload")]
    Task<Guid> UploadAsync(
        [Form("file")] RemoteUploadFile file,
        CancellationToken cancellationToken = default);

    [Get("{id}/content")]
    Task<RemoteFile> DownloadAsync(
        [Path] Guid id,
        CancellationToken cancellationToken = default);
}
```

Remote methods must return `Task`, `Task<T>`, `ValueTask`, or `ValueTask<T>`. A `CancellationToken` is treated as infrastructure: the generated client passes it to `HttpClient`, and the generated server handler receives the request cancellation token.

## Binding attributes

| Attribute | Purpose |
| --- | --- |
| `[Path]` | Binds a value to a route parameter. |
| `[Query]` | Binds a scalar, collection, or supported query DTO to the query string. |
| `[Header]` | Binds a scalar value to an HTTP header. |
| `[Body]` | Selects the request body and optional media type. |
| `[Form]` | Selects a form field or uploaded file. |

Binding is inferred when it is unambiguous. Explicit attributes are recommended at public API boundaries because they make the wire contract visible during code review.

## Multiple HTTP methods

A method may expose more than one HTTP operation. Exactly one operation must be the generated client's default:

```csharp
[Get("items", IsClientDefault = true)]
[Post("items/search")]
Task<Item[]> GetItemsAsync(string? query, CancellationToken cancellationToken = default);
```

The server generator maps both endpoints. The client uses the default unless the caller selects another method through the Client package.

## Endpoint policy attributes

The contract may select named authorization, CORS, output cache, rate limit, request timeout, and custom endpoint policies without referencing ASP.NET Core:

```csharp
[RemoteService("/api/weather")]
[RemoteAuthorize("ApiUser", Roles = "WeatherReader,Administrator")]
[RemoteCors("Frontend")]
public interface IWeatherService
{
    [Get]
    [RemoteOutputCache("WeatherQueries")]
    Task<WeatherForecast[]> GetAsync();
}
```

Policy definitions remain in the ASP.NET Core host. Roles in one comma-delimited list are
alternatives; role groups declared by separate attributes must all be satisfied.

## Endpoint metadata

Services and methods can declare ASP.NET Core Minimal API endpoint metadata. Attributes that support HTTP method selection can set `HttpMethod` to target one operation; omitting it applies the metadata to every operation declared by the contract:

```csharp
[RemoteService("/api/catalog")]
[RemoteTags("catalog")]
public interface ICatalogService
{
    [Get(IsClientDefault = true), Post("search")]
    [RemoteSummary("Lists catalog entries", HttpMethod = RemoteHttpMethod.Get)]
    [RemoteProducesProblem(400, HttpMethod = RemoteHttpMethod.Post)]
    Task<Item[]> FindAsync([Query] string? query);
}
```

Version 2.0 also provides `RemoteProduces<T>`, `RemoteAccepts<T>`, `RemoteProducesProblem`, `RemoteProducesValidationProblem`, `RemoteDescription`, `RemoteExcludeFromDescription`, `RemoteHost`, `RemoteOrder`, `RemoteDisplayName`, `RemoteEndpointName`, `RemoteDisableAntiforgery`, `RemoteFormOptions`, and `RemoteFormMappingOptions`. The .NET 10-only `RemoteDisableValidation` and `RemoteAllowCookieRedirect` attributes produce a generator warning and skip that configuration when the host targets an earlier ASP.NET Core version.

Service interfaces can inherit shared base contracts. The generator merges methods, policies, OpenAPI metadata, and parameter bindings through the interface hierarchy; a redeclared method on the derived interface can replace its HTTP operation declarations. Conflicting HTTP operations declared for the same method by unrelated base interfaces produce a contract diagnostic and require an explicit redeclaration on the service interface.

## Analyzer diagnostics

The package reports invalid contracts while editing and building, including synchronous methods, missing HTTP methods, unsupported routes and signatures, ambiguous client defaults, and duplicate HTTP operations. Invalid services do not publish a contract manifest for downstream generators.

## Related packages

- `Ling.RemoteServices.Client` generates and runs type-safe client proxies.
- `Ling.RemoteServices.AspNetCore` generates and maps Minimal API endpoints.

## License

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
