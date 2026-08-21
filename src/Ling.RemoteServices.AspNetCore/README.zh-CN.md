# Ling.RemoteServices.AspNetCore

[English](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices.AspNetCore/README.md) | 简体中文 | [项目文档](https://github.com/ling921/ling-remote-services/blob/master/README.zh-CN.md)

`Ling.RemoteServices.AspNetCore` 为标记了 `[RemoteService]` 的接口生成 ASP.NET Core Minimal API Route Group 和 handler。生成的端点可与依赖注入、原生 endpoint convention、Problem Details、文件结果和 OpenAPI metadata 集成。

## 安装

该包尚未发布。首次发布后，请在 ASP.NET Core 宿主中安装：

```shell
dotnet add package Ling.RemoteServices.AspNetCore
```

该包会引用 `Ling.RemoteServices.Abstractions`，并携带 Minimal API 源代码生成器。

## 注册并映射服务

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

每个远程服务都会获得独立的生成 `RouteGroupBuilder`。所有声明的 HTTP Method 都会被映射为独立端点，并从依赖注入中解析契约实现。

## Native AOT

请注册一个包含全部 JSON 请求和响应类型的 System.Text.Json 源生成上下文：

```csharp
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        RemoteServiceJsonContext.Default);
});
```

在 JIT 宿主中，生成端点会保留 ASP.NET Core 原生参数推断和 OpenAPI metadata 行为；当动态代码不可用时，同一个生成映射器会自动选择无反射的 `RequestDelegate`，直接绑定 Path、Query、Header、JSON Body、Form、文件和 CancellationToken。包程序集已声明支持裁剪和 AOT。

## 端点策略

契约策略特性用于选择宿主中定义的策略名称。对应的 ASP.NET Core 服务和 Middleware 仍按原生方式配置：

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

自定义命名端点策略可以组合不需要专属契约特性的 convention：

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

Form 和文件端点会保留 ASP.NET Core antiforgery metadata，本库不会自动禁用防伪验证。

## Fluent Convention

`MapRemoteServices()` 返回一个实现了 `IEndpointConventionBuilder` 的注册表：

```csharp
app.MapRemoteServices(services =>
{
    var weather = services.For<IWeatherService>();

    weather.RequireAuthorization("WeatherReader");
    weather.Operation(nameof(IWeatherService.GetAsync))
        .CacheOutput("WeatherQueries");
});
```

当一个契约方法公开多个 Method 时，可以使用 `Operation(methodName, RemoteHttpMethod)` 只配置其中一个 HTTP 操作。

## OpenAPI

生成端点会附加原生 Minimal API metadata，例如 operation name、summary、接受的内容类型、响应类型和参数绑定来源。请配置目标 ASP.NET Core 版本提供的 OpenAPI 组件：

```csharp
builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapRemoteServices();
```

## 错误处理

`RemoteServiceException` 会转换成 `application/problem+json` 响应。未知异常会继续交给宿主的 `IExceptionHandler`、开发异常页面和日志 Pipeline 处理。

## 协议

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
