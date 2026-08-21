# Ling.RemoteServices.Client

[English](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices.Client/README.md) | 简体中文 | [项目文档](https://github.com/ling921/ling-remote-services/blob/master/README.zh-CN.md)

`Ling.RemoteServices.Client` 为标记了 `[RemoteService]` 的接口生成类型安全的 `HttpClient` 代理。它提供 URL 格式化、System.Text.Json 序列化、流式下载和结构化远程调用异常，并且不依赖 MVC 或 `Microsoft.Extensions.Http`。

## 安装

请在 Blazor WebAssembly 或其他客户端项目中安装预览包：

```shell
dotnet add package Ling.RemoteServices.Client --prerelease
```

该包会引用 `Ling.RemoteServices.Abstractions`，并携带客户端代理源代码生成器。

## 注册生成的客户端

调用生成的扩展方法之前，需要先注册一个 `HttpClient`。生成器会将注册方法放在 `${RootNamespace}.Generated` 命名空间中：

```csharp
using MyApplication.Generated;

var builder = WebAssemblyHostBuilder.CreateDefault(args);

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddRemoteServiceClients();
```

生成的代理会作为对应契约接口的 Scoped 实现注册。组件和服务可以直接注入接口：

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

## 配置序列化

```csharp
builder.Services.AddRemoteServiceClients(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.MaximumErrorBodySize = 64 * 1024;
    options.EnableBrowserRequestStreaming = true;
});
```

客户端 JSON 选项应与服务端 ASP.NET Core `HttpJsonOptions` 保持兼容，特别是在使用自定义 Converter 或多态序列化时。

### Native AOT JSON Metadata

Native AOT 应用必须使用 System.Text.Json 源生成。请将序列化上下文放在共享契约程序集中，并在客户端注册：

```csharp
[JsonSerializable(typeof(WeatherForecast[]))]
public sealed partial class RemoteServiceJsonContext : JsonSerializerContext
{
}

builder.Services.AddRemoteServiceClients(options =>
    options.AddJsonSerializerContext(RemoteServiceJsonContext.Default));
```

生成代理会把强类型 `JsonTypeInfo<T>` 传给 `JsonContent` 和响应反序列化 API。客户端包自身已经包含协议内部 Problem Details 的元数据。

## 选择其他 HTTP Method

当契约方法声明多个 HTTP 操作时，普通接口调用会使用标记了 `IsClientDefault = true` 的操作。可以通过不可变的代理视图选择另一个已生成操作：

```csharp
var items = await service
    .WithHttpMethod(RemoteHttpMethod.Post)
    .GetItemsAsync(query, cancellationToken);
```

本地实现会保持不变，因此同一份组件代码可以在 Interactive Server 渲染期间使用本地实现，并在切换到 WebAssembly 后使用生成代理。

## 错误和流式文件

- 非成功响应会产生 `RemoteCallException`，其中包含状态码、Problem Details、响应头、操作名称和受大小限制的响应摘要。
- 网络、协议和反序列化错误会产生 `RemoteTransportException`。
- 调用方取消操作时仍表现为 `OperationCanceledException`。
- `RemoteFile` 持有流式下载 HTTP 响应的生命周期，调用方必须对其执行同步或异步释放。

## HttpClient Pipeline

认证、Cookie、Bearer Token、重试、日志和链路追踪应配置在宿主提供的 `HttpClient` 上。生成代理会复用该实例，而不会建立另一套网络栈。

## 协议

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
