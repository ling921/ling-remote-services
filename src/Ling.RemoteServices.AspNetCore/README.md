# Ling.RemoteServices.AspNetCore

[Project documentation](https://github.com/ling921/ling-remote-services#readme) | [简体中文](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices.AspNetCore/README.zh-CN.md)

`Ling.RemoteServices.AspNetCore` generates ASP.NET Core Minimal API route groups and handlers for interfaces marked with `[RemoteService]`. It integrates generated endpoints with dependency injection, native endpoint conventions, Problem Details, file results, and OpenAPI metadata.

## Installation

Install the preview package in the ASP.NET Core host:

```shell
dotnet add package Ling.RemoteServices.AspNetCore --prerelease
```

The package references `Ling.RemoteServices.Abstractions` and carries the Minimal API source generator.

## Register and map a service

```csharp
using Ling.RemoteServices.AspNetCore;
using MyApplication.Generated;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRemoteServices();
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();
app.MapRemoteServices();
app.Run();
```

Each remote service receives its own generated `RouteGroupBuilder`. Every declared HTTP method is mapped as an independent endpoint and resolves the contract implementation from dependency injection.

## Native AOT

Register a source-generated System.Text.Json context containing every JSON request and response type:

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        RemoteServiceJsonContext.Default);
});
```

On JIT hosts, generated endpoints retain ASP.NET Core's native parameter inference and OpenAPI metadata behavior. When dynamic code is unavailable, the same generated mapper selects a reflection-free `RequestDelegate` that binds path, query, header, JSON body, form, upload, and cancellation values directly. The package assemblies declare trimming and AOT compatibility.

## Endpoint policies

Contract policy attributes select host-defined policy names. Configure the corresponding ASP.NET Core services and middleware normally:

```csharp
builder.Services.AddAuthorization(options =>
    options.AddPolicy("ApiUser", policy => policy.RequireAuthenticatedUser()));

builder.Services.AddCors(options =>
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins("https://example.com")
            .AllowAnyHeader()
            .AllowAnyMethod()));

builder.Services.AddOutputCache(options =>
    options.AddPolicy("WeatherQueries", policy =>
        policy.Expire(TimeSpan.FromSeconds(30))));
```

Custom named endpoint policies can combine conventions that do not need a dedicated contract attribute:

```csharp
builder.Services.AddRemoteServices(options =>
{
    options.AddEndpointPolicy("PublicQuery", context =>
    {
        context.RequireCors("Frontend");
        context.CacheOutput("WeatherQueries");
    });
});
```

Form and file endpoints retain ASP.NET Core antiforgery metadata. The library does not disable antiforgery automatically.

## Fluent conventions

`MapRemoteServices()` returns a registry implementing `IEndpointConventionBuilder`:

```csharp
app.MapRemoteServices(services =>
{
    var weather = services.For<IWeatherService>();

    weather.RequireAuthorization("WeatherReader");
    weather.Operation(nameof(IWeatherService.GetAsync))
        .CacheOutput("WeatherQueries");
});
```

Use `Operation(methodName, RemoteHttpMethod)` to target one HTTP operation when a contract method exposes multiple methods.

## OpenAPI

Generated endpoints attach native Minimal API metadata such as operation names, summaries, accepted content types, produced responses, and binding sources. Configure the OpenAPI stack supplied by the target ASP.NET Core version:

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapRemoteServices();
```

## Error handling

`RemoteServiceException` is translated to an `application/problem+json` response. Unknown exceptions are intentionally left to the host's `IExceptionHandler`, developer exception page, and logging pipeline.

## License

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
