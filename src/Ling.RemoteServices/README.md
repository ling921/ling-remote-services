# Ling.RemoteServices.Abstractions

[Project documentation](https://github.com/ling921/ling-remote-services#readme) | [简体中文](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices/README.zh-CN.md)

`Ling.RemoteServices.Abstractions` contains the transport-independent contract surface for Ling.RemoteServices. It includes contract attributes, shared file and error models, remote exceptions, a contract analyzer, and the contract manifest source generator. It does not reference MVC or ASP.NET Core.

## Installation

This package has not been published yet. After the first release, install it in the project that owns the shared service interfaces:

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
[RemoteAuthorize("ApiUser")]
[RemoteCors("Frontend")]
public interface IWeatherService
{
    [Get]
    [RemoteOutputCache("WeatherQueries")]
    Task<WeatherForecast[]> GetAsync();
}
```

Policy definitions remain in the ASP.NET Core host.

## Analyzer diagnostics

The package reports invalid contracts while editing and building, including synchronous methods, missing HTTP methods, unsupported routes and signatures, ambiguous client defaults, and duplicate HTTP operations. Invalid services do not publish a contract manifest for downstream generators.

## Related packages

- `Ling.RemoteServices.Client` generates and runs type-safe client proxies.
- `Ling.RemoteServices.AspNetCore` generates and maps Minimal API endpoints.

## License

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
