# Ling.RemoteServices.Client

[Project documentation](https://github.com/ling921/ling-remote-services#readme) | [简体中文](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices.Client/README.zh-CN.md)

`Ling.RemoteServices.Client` generates type-safe `HttpClient` proxies for interfaces marked with `[RemoteService]`. It provides URL formatting, System.Text.Json serialization, streamed downloads, and structured remote-call exceptions without depending on MVC or `Microsoft.Extensions.Http`.

## Installation

Install the preview package in the Blazor WebAssembly or other client project:

```shell
dotnet add package Ling.RemoteServices.Client --prerelease
```

The package references `Ling.RemoteServices.Abstractions` and carries the client proxy source generator.

## Register generated clients

Register an `HttpClient` before calling the generated extension method. The generator places the method in `${RootNamespace}.Generated`:

```csharp
using MyApplication.Generated;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddRemoteServiceClients();
```

Generated proxies are registered as scoped implementations of their contract interfaces. Components and services can inject the interface directly:

```razor
@inject IWeatherService WeatherService

@code {
    private WeatherForecast[] forecasts = [];

    protected override async Task OnInitializedAsync()
    {
        forecasts = await WeatherService.GetAsync();
    }
}
```

## Configure serialization

```csharp
builder.Services.AddRemoteServiceClients(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.MaximumErrorBodySize = 64 * 1024;
    options.EnableBrowserRequestStreaming = true;
});
```

Keep the client JSON options compatible with the server's ASP.NET Core `HttpJsonOptions`, especially when using custom converters or polymorphism.

### Native AOT JSON metadata

Native AOT applications must use System.Text.Json source generation. Put the serializer context in the shared contract assembly and register it with the client:

```csharp
[JsonSerializable(typeof(WeatherForecast[]))]
public sealed partial class RemoteServiceJsonContext : JsonSerializerContext
{
}

builder.Services.AddRemoteServiceClients(options =>
    options.AddJsonSerializerContext(RemoteServiceJsonContext.Default));
```

Generated proxies pass strongly typed `JsonTypeInfo<T>` instances to `JsonContent` and response deserialization. Protocol-owned Problem Details metadata is included by the client package itself.

## Select another HTTP method

When a contract method declares multiple HTTP operations, the normal interface call uses the operation marked `IsClientDefault = true`. Select another generated operation through an immutable proxy view:

```csharp
var items = await service
    .WithHttpMethod(RemoteHttpMethod.Post)
    .GetItemsAsync(query, cancellationToken);
```

Local implementations are returned unchanged, which allows the same component code to run with a local implementation during Interactive Server rendering and a generated proxy after switching to WebAssembly.

## Errors and streamed files

- Non-success responses produce `RemoteCallException` with status, Problem Details, headers, operation name, and a bounded response summary.
- Network, protocol, and deserialization failures produce `RemoteTransportException`.
- Caller cancellation remains `OperationCanceledException`.
- `RemoteFile` owns the HTTP response lifetime for streamed downloads and must be disposed or asynchronously disposed by the caller.

## HttpClient pipeline

Authentication, cookies, bearer tokens, retry, logging, and tracing should be configured on the `HttpClient` supplied by the host. Generated proxies reuse that instance rather than creating a separate networking stack.

## License

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
