# Ling.RemoteServices

[English](README.md) | 简体中文

`Ling.RemoteServices` 是一个面向 .NET 8–10 的契约优先源代码生成器。它根据共享的 C# 接口生成 ASP.NET Core Minimal API 端点和类型安全的 `HttpClient` 代理，并针对 Blazor Auto 应用提供良好支持。

## NuGet 包

预览包已使用以下包 ID 发布到 NuGet.org。

| 包 | 用途 | 状态 |
| --- | --- | --- |
| [`Ling.RemoteServices.Abstractions`](src/Ling.RemoteServices/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.Abstractions.svg)](https://www.nuget.org/packages/Ling.RemoteServices.Abstractions/) | 与传输无关的契约、特性、模型、异常、分析器和契约清单生成器。 | 预览版 |
| [`Ling.RemoteServices.Client`](src/Ling.RemoteServices.Client/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.Client.svg)](https://www.nuget.org/packages/Ling.RemoteServices.Client/) | 生成的 `HttpClient` 代理、URL 格式化、序列化和客户端错误处理。 | 预览版 |
| [`Ling.RemoteServices.AspNetCore`](src/Ling.RemoteServices.AspNetCore/README.md) [![NuGet](https://img.shields.io/nuget/v/Ling.RemoteServices.AspNetCore.svg)](https://www.nuget.org/packages/Ling.RemoteServices.AspNetCore/) | 生成的 Minimal API 映射、端点约定、策略和服务端错误处理。 | 预览版 |

## 功能

- 共享契约接口不依赖 MVC 或 ASP.NET Core。
- 分别生成契约清单、客户端代理和 Minimal API 端点的增量生成器。
- 在设计时检查无效契约的 Roslyn 分析器诊断。
- 每个远程服务分别生成一个客户端代理和一个端点映射器。
- 支持 Path、Query、Header、JSON Body、Form、文件上传和流式下载模型。
- 一个契约方法可以声明多个 HTTP Method，并显式选择客户端默认 Method。
- 与 ASP.NET Core 原生授权、CORS、输出缓存、限流、请求超时、防伪和 OpenAPI metadata 集成。
- 继续复用现有的 `HttpClient` pipeline 和 endpoint convention，以配置认证、重试、日志、链路追踪及自定义策略。
- 客户端序列化和服务端 RequestDelegate 支持裁剪及 Native AOT。
- 支持 .NET 8、.NET 9 和 .NET 10。

## 使用方式

在引用 `Ling.RemoteServices.Abstractions` 的共享项目中声明契约：

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

客户端项目引用 `Ling.RemoteServices.Client`，注册已有的 `HttpClient`，然后调用生成的注册方法：

```csharp
using MyApplication.Generated;

builder.Services.AddScoped(_ => new HttpClient
{
    BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
});
builder.Services.AddRemoteServiceClients();
```

ASP.NET Core 宿主引用 `Ling.RemoteServices.AspNetCore`，注册接口实现并映射生成的端点：

```csharp
using MyApplication.Generated;

builder.Services.AddRemoteServices();
builder.Services.AddScoped<IWeatherService, WeatherService>();

var app = builder.Build();
app.MapRemoteServices();
```

## Native AOT

Native AOT 会禁用 JSON 反射。请在共享契约项目中声明一个 System.Text.Json 源生成上下文，并包含所有 JSON Body 和响应类型：

```csharp
using System.Text.Json.Serialization;

[JsonSerializable(typeof(WeatherForecast))]
[JsonSerializable(typeof(WeatherForecast[]))]
public sealed partial class RemoteServiceJsonContext : JsonSerializerContext
{
}
```

客户端和服务端注册同一个上下文：

```csharp
// Client
builder.Services.AddRemoteServiceClients(options =>
    options.AddJsonSerializerContext(RemoteServiceJsonContext.Default));

// ASP.NET Core Server
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.TypeInfoResolverChain.Insert(
        0,
        RemoteServiceJsonContext.Default));
```

普通 JIT 宿主仍使用 ASP.NET Core 原生参数推断；Native AOT 环境下，服务端生成器会自动选择无反射的生成 `RequestDelegate` 绑定逻辑。仓库中的 `tests/Ling.RemoteServices.NativeAotSmoke` 会执行零警告发布和 JSON 往返验证。

安装、参数绑定、客户端选项、端点策略和生成 API 的详细说明，请查看[包列表](#nuget-包)中的各包 README。

## 生成器架构

- `Ling.RemoteServices.Generators.Contract` 验证源码契约并生成程序集清单，随 `Ling.RemoteServices.Abstractions` 一起分发。
- `Ling.RemoteServices.Generators.Client` 从源码或引用的清单中发现契约并生成客户端代理，随 `Ling.RemoteServices.Client` 一起分发。
- `Ling.RemoteServices.Generators.AspNetCore` 生成 Minimal API Route Group 和端点，随 `Ling.RemoteServices.AspNetCore` 一起分发。
- 共享的解析、诊断、路由和中间模型通过 Visual Studio Shared Project 编译进每个生成器，避免额外的 Roslyn 加载依赖。

## 开发

- 构建：`dotnet build Ling.RemoteServices.slnx`
- 测试（包含 Native AOT 发布和往返验证）：Windows 使用 `./scripts/test.ps1`，Linux 和 macOS 使用 `sh ./scripts/test.sh`。
- 完整测试通过后打包三个公开包：Windows 使用 `./scripts/pack.ps1`，Linux 和 macOS 使用 `sh ./scripts/pack.sh`。
- 如需覆盖自动检测的 RID，可向 PowerShell 脚本传入 `-RuntimeIdentifier <RID>`，或向 Shell 脚本传入 `--runtime <RID>`。
- Blazor Auto 示例位于 `samples/Ling.RemoteServices.Examples`。

## 后续计划

- 提供契约演进和公开 API 兼容性诊断，识别路由、绑定与类型的破坏性变更。
- 扩充路由、文件、OpenAPI、Native AOT 和受支持 .NET 版本的端到端测试。
- 丰富成功与错误响应、Header、分页、Query DTO 和流式结果等 HTTP 契约。

## 参与贡献

欢迎提交 Issue 或 Pull Request。行为变更应同时包含对应的测试。

## 协议

- [MIT License](LICENSE)
