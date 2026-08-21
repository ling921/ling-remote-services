# Ling.RemoteServices

English | [简体中文](README.zh-CN.md)

`Ling.RemoteServices` is a contract-first source generator for .NET 8–10. It generates ASP.NET Core Minimal API endpoints and type-safe `HttpClient` proxies from shared C# interfaces, with first-class support for Blazor Auto applications.

## Packages

Preview packages are published to NuGet.org under the following package IDs.

| Package | Purpose | Status |
| --- | --- | --- |
| [`Ling.RemoteServices.Abstractions`](src/Ling.RemoteServices/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.Abstractions.svg)](https://www.nuget.org/packages/Ling.RemoteServices.Abstractions/) | Transport-independent contracts, attributes, models, exceptions, analyzer, and contract manifest generator. | Preview |
| [`Ling.RemoteServices.Client`](src/Ling.RemoteServices.Client/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.Client.svg)](https://www.nuget.org/packages/Ling.RemoteServices.Client/) | Generated `HttpClient` proxies, URL formatting, serialization, and client error handling. | Preview |
| [`Ling.RemoteServices.AspNetCore`](src/Ling.RemoteServices.AspNetCore/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.AspNetCore.svg)](https://www.nuget.org/packages/Ling.RemoteServices.AspNetCore/) | Generated Minimal API mappings, endpoint conventions, policies, and server error handling. | Preview |

## Features

- Shared contract interfaces with no MVC or ASP.NET Core dependency.
- Incremental generators for contract manifests, client proxies, and Minimal API endpoints.
- Roslyn analyzer diagnostics for invalid contracts at design time.
- One generated client proxy and one generated endpoint mapper per remote service.
- Path, query, header, JSON body, form, upload, and streaming download models.
- Multiple HTTP methods per contract method with an explicit client default.
- Native ASP.NET Core authorization, CORS, output cache, rate limit, request timeout, antiforgery, and OpenAPI metadata integration.
- Existing `HttpClient` pipelines and endpoint conventions remain available for authentication, retry, logging, tracing, and custom policies.
- Trimming and Native AOT compatible client serialization and server request delegates.
- .NET 8, .NET 9, and .NET 10 support.

## Usage

Create a shared contract that references `Ling.RemoteServices.Abstractions`:

```csharp
using Ling.RemoteServices.Attributes;

[RemoteService("/api/weather")]
public interface IWeatherService
{
    [Get("{day}")]
    Task<WeatherForecast> GetAsync(
        [Path] DateOnly day,
        CancellationToken cancellationToken = default);
}
```

In the client project, reference `Ling.RemoteServices.Client`, register the existing `HttpClient`, and call the generated registration method:

```csharp
using MyApplication.Generated;

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddRemoteServiceClients();
```

In the ASP.NET Core host, reference `Ling.RemoteServices.AspNetCore`, register the implementation, and map the generated endpoints:

```csharp
using MyApplication.Generated;

builder.Services.AddRemoteServices();
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();
app.MapRemoteServices();
```

## Native AOT

JSON reflection is disabled by Native AOT. Define one source-generated serializer context in the shared contract project and include every JSON body and response type:

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
public sealed partial class RemoteServiceJsonContext : JsonSerializerContext
{
}
```

Register the same context on both sides:

```csharp
// Client
builder.Services.AddRemoteServiceClients(options =>
    options.AddJsonSerializerContext(RemoteServiceJsonContext.Default));

// ASP.NET Core server
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        RemoteServiceJsonContext.Default));
```

The server generator preserves ASP.NET Core's normal parameter inference on JIT hosts and automatically selects its reflection-free generated `RequestDelegate` binder under Native AOT. The repository includes a warning-free publish and JSON round-trip smoke project under `tests/Ling.RemoteServices.NativeAotSmoke`.

For installation, binding, client options, endpoint policies, and generated API details, see the package-specific READMEs in the [package list](#packages).

## Generator architecture

- `Ling.RemoteServices.Generators.Contract` validates source contracts and emits assembly manifests. It ships inside `Ling.RemoteServices.Abstractions`.
- `Ling.RemoteServices.Generators.Client` discovers source or referenced manifests and emits client proxies. It ships inside `Ling.RemoteServices.Client`.
- `Ling.RemoteServices.Generators.AspNetCore` emits Minimal API route groups and endpoints. It ships inside `Ling.RemoteServices.AspNetCore`.
- Shared parsing, diagnostics, routing, and intermediate models are compiled into each generator through a Visual Studio Shared Project, avoiding additional Roslyn load dependencies.

## Development

- Build: `dotnet build Ling.RemoteServices.slnx`
- Test, including the Native AOT publish and round trip: `./scripts/test.ps1` on Windows or `sh ./scripts/test.sh` on Linux and macOS.
- Pack all three public packages after running the full test suite: `./scripts/pack.ps1` on Windows or `sh ./scripts/pack.sh` on Linux and macOS.
- Pass `-RuntimeIdentifier <RID>` to the PowerShell scripts or `--runtime <RID>` to the shell scripts to override automatic RID detection.
- The Blazor Auto sample is under `samples/Ling.RemoteServices.Examples`.

## Planned enhancements

- Contract evolution and public API compatibility diagnostics for breaking route, binding, and type changes.
- Broader end-to-end coverage for routing, files, OpenAPI, Native AOT, and supported .NET versions.
- Richer HTTP contracts for success and error responses, headers, pagination, query DTOs, and streaming results.

## Contributing

Contributions are welcome. Please open an issue or pull request and include tests for behavior changes.

## License

- [MIT License](LICENSE)
