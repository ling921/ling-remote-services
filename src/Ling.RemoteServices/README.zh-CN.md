# Ling.RemoteServices.Abstractions

[English](https://github.com/ling921/ling-remote-services/blob/master/src/Ling.RemoteServices/README.md) | 简体中文 | [项目文档](https://github.com/ling921/ling-remote-services/blob/master/README.zh-CN.md)

`Ling.RemoteServices.Abstractions` 提供 Ling.RemoteServices 与传输无关的契约定义。它包含契约特性、共享文件及错误模型、远程异常、契约分析器和契约清单源代码生成器，并且不引用 MVC 或 ASP.NET Core。

## 安装

该包尚未发布。首次发布后，请在定义共享服务接口的项目中安装：

```shell
dotnet add package Ling.RemoteServices.Abstractions
```

## 定义契约

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

远程方法必须返回 `Task`、`Task<T>`、`ValueTask` 或 `ValueTask<T>`。`CancellationToken` 被视为基础设施参数：生成的客户端会将它传给 `HttpClient`，服务端 handler 则会接收请求的取消令牌。

## 参数绑定特性

| 特性 | 用途 |
| --- | --- |
| `[Path]` | 将值绑定到路由参数。 |
| `[Query]` | 将标量、集合或受支持的 Query DTO 绑定到查询字符串。 |
| `[Header]` | 将标量值绑定到 HTTP Header。 |
| `[Body]` | 指定请求 Body 及可选的媒体类型。 |
| `[Form]` | 指定表单字段或上传文件。 |

当绑定来源不存在歧义时，生成器会进行推断。对于公开 API，建议显式使用绑定特性，使代码审查时可以直接看到线上协议。

## 多个 HTTP Method

同一个方法可以公开多个 HTTP 操作，但必须且只能将其中一个标记为生成客户端的默认操作：

```csharp
[Get("items", IsClientDefault = true)]
[Post("items/search")]
Task<Item[]> GetItemsAsync(string? query, CancellationToken cancellationToken = default);
```

服务端生成器会映射所有端点。客户端默认使用 `IsClientDefault` 指定的操作，也可以通过 Client 包显式选择其他 Method。

## 端点策略特性

契约可以选择命名的授权、CORS、输出缓存、限流、请求超时和自定义端点策略，同时保持对 ASP.NET Core 的零引用：

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

具体策略仍由 ASP.NET Core 宿主定义。

## 分析器诊断

该包会在编辑和构建阶段报告无效契约，包括同步方法、缺少 HTTP Method、不受支持的路由或方法签名、存在歧义的客户端默认操作以及重复 HTTP 操作。无效服务不会向下游生成器发布契约清单。

## 相关包

- `Ling.RemoteServices.Client` 生成并运行类型安全的客户端代理。
- `Ling.RemoteServices.AspNetCore` 生成并映射 Minimal API 端点。

## 协议

[MIT](https://github.com/ling921/ling-remote-services/blob/master/LICENSE)
